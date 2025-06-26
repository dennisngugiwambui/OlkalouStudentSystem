// ===============================
// Services/AuthService.cs - Updated with Supabase Integration
// ===============================
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Models.Data;
using System.Text.Json;

namespace OlkalouStudentSystem.Services
{
    public class AuthService
    {
        private readonly Supabase.Client _supabaseClient;
        private Student _currentStudent;
        private string _authToken;
        private string _refreshToken;
        private DateTime _tokenExpiry;

        // Events for authentication state changes
        public event EventHandler<AuthenticationStateChangedEventArgs> AuthenticationStateChanged;

        // Constants for secure storage keys
        private const string AUTH_TOKEN_KEY = "auth_token";
        private const string REFRESH_TOKEN_KEY = "refresh_token";
        private const string STUDENT_DATA_KEY = "student_data";
        private const string TOKEN_EXPIRY_KEY = "token_expiry";
        private const string USER_ID_KEY = "user_id";
        private const string USER_TYPE_KEY = "user_type";

        public AuthService()
        {
            _supabaseClient = SupabaseService.Instance;
        }

        #region Public Properties

        /// <summary>
        /// Gets the currently authenticated student
        /// </summary>
        public Student CurrentStudent => _currentStudent;

        /// <summary>
        /// Gets the current authentication token
        /// </summary>
        public string AuthToken => _authToken;

        /// <summary>
        /// Gets whether the user is currently authenticated
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken) &&
                                      _currentStudent != null &&
                                      DateTime.Now < _tokenExpiry;

        /// <summary>
        /// Gets whether the token is about to expire (within 5 minutes)
        /// </summary>
        public bool IsTokenExpiringSoon => IsAuthenticated &&
                                          DateTime.Now.AddMinutes(5) >= _tokenExpiry;

        /// <summary>
        /// Gets the current user's role/permissions
        /// </summary>
        public string UserRole => _currentStudent?.Form ?? "Student";

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Authenticate user with phone number and password
        /// </summary>
        /// <param name="phoneNumber">User's phone number</param>
        /// <param name="password">User's password</param>
        /// <returns>Login response with success status and user data</returns>
        public async Task<LoginResponse> LoginAsync(string phoneNumber, string password)
        {
            try
            {
                // Validate input parameters
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    throw new ArgumentException("Phone number is required", nameof(phoneNumber));

                if (string.IsNullOrWhiteSpace(password))
                    throw new ArgumentException("Password is required", nameof(password));

                // Normalize phone number format
                var normalizedPhone = NormalizePhoneNumber(phoneNumber);

                // Query Supabase for user with matching credentials
                var userResponse = await _supabaseClient
                    .From<UserEntity>()
                    .Where(x => x.PhoneNumber == normalizedPhone && x.Password == password && x.IsActive)
                    .Single();

                if (userResponse == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "Invalid phone number or password",
                        ErrorCode = "AUTH_INVALID_CREDENTIALS"
                    };
                }

                // Update last login
                userResponse.LastLogin = DateTime.UtcNow;
                await userResponse.Update<UserEntity>();

                // Get user profile based on user type
                var student = await GetStudentProfileAsync(userResponse);

                if (student == null)
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = "User profile not found",
                        ErrorCode = "PROFILE_NOT_FOUND"
                    };
                }

                // Store authentication data
                _currentStudent = student;
                _authToken = GenerateAuthToken();
                _refreshToken = GenerateRefreshToken();
                _tokenExpiry = DateTime.Now.AddHours(24); // Token valid for 24 hours

                // Persist authentication data securely
                await SaveAuthenticationDataAsync();

                // Notify authentication state change
                OnAuthenticationStateChanged(true, _currentStudent);

                return new LoginResponse
                {
                    Success = true,
                    Token = _authToken,
                    Student = student,
                    Message = "Login successful"
                };
            }
            catch (Exception ex)
            {
                // Log error (in production, use proper logging)
                System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");

                return new LoginResponse
                {
                    Success = false,
                    Message = $"Login failed: {ex.Message}",
                    ErrorCode = "LOGIN_EXCEPTION"
                };
            }
        }

        /// <summary>
        /// Get student profile from Supabase based on user entity
        /// </summary>
        private async Task<Student> GetStudentProfileAsync(UserEntity userEntity)
        {
            try
            {
                var student = new Student();

                switch (userEntity.UserType)
                {
                    case "Student":
                        var studentEntity = await _supabaseClient
                            .From<StudentEntity>()
                            .Where(x => x.UserId == userEntity.Id)
                            .Single();

                        if (studentEntity != null)
                        {
                            student = new Student
                            {
                                StudentId = studentEntity.StudentId,
                                FullName = studentEntity.FullName,
                                AdmissionNo = studentEntity.AdmissionNo,
                                Form = studentEntity.Form,
                                Class = studentEntity.Class,
                                Email = studentEntity.Email,
                                PhoneNumber = userEntity.PhoneNumber,
                                DateOfBirth = studentEntity.DateOfBirth,
                                Gender = studentEntity.Gender,
                                Address = studentEntity.Address
                            };
                        }
                        break;

                    case "Teacher":
                        var teacherEntity = await _supabaseClient
                            .From<TeacherEntity>()
                            .Where(x => x.UserId == userEntity.Id)
                            .Single();

                        if (teacherEntity != null)
                        {
                            student = new Student
                            {
                                StudentId = teacherEntity.TeacherId,
                                FullName = teacherEntity.FullName,
                                Form = "Teacher",
                                Class = "Staff",
                                PhoneNumber = userEntity.PhoneNumber
                            };
                        }
                        break;

                    case "Principal":
                    case "DeputyPrincipal":
                    case "Secretary":
                    case "Bursar":
                        var staffEntity = await _supabaseClient
                            .From<StaffEntity>()
                            .Where(x => x.UserId == userEntity.Id)
                            .Single();

                        if (staffEntity != null)
                        {
                            student = new Student
                            {
                                StudentId = staffEntity.StaffId,
                                FullName = staffEntity.FullName,
                                Form = staffEntity.Position,
                                Class = "Staff",
                                PhoneNumber = userEntity.PhoneNumber
                            };
                        }
                        break;
                }

                return student;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get student profile error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Logout the current user and clear all authentication data
        /// </summary>
        public async Task LogoutAsync()
        {
            try
            {
                var wasAuthenticated = IsAuthenticated;
                var previousStudent = _currentStudent;

                // Clear in-memory data
                _currentStudent = null;
                _authToken = null;
                _refreshToken = null;
                _tokenExpiry = DateTime.MinValue;

                // Clear persisted data
                await ClearAuthenticationDataAsync();

                // Notify authentication state change
                if (wasAuthenticated)
                {
                    OnAuthenticationStateChanged(false, previousStudent);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw - logout should always succeed locally
                System.Diagnostics.Debug.WriteLine($"Logout error: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if user is currently logged in
        /// </summary>
        /// <returns>True if user is authenticated</returns>
        public async Task<bool> IsLoggedInAsync()
        {
            // If already authenticated in memory, return true
            if (IsAuthenticated)
                return true;

            // Try to restore authentication from secure storage
            return await TryRestoreAuthenticationAsync();
        }

        /// <summary>
        /// Attempt to restore authentication state from secure storage
        /// </summary>
        /// <returns>True if authentication was restored successfully</returns>
        public async Task<bool> TryRestoreAuthenticationAsync()
        {
            try
            {
                // Retrieve stored authentication data
                var authToken = await SecureStorage.GetAsync(AUTH_TOKEN_KEY);
                var studentDataJson = await SecureStorage.GetAsync(STUDENT_DATA_KEY);
                var tokenExpiryString = await SecureStorage.GetAsync(TOKEN_EXPIRY_KEY);
                var userId = await SecureStorage.GetAsync(USER_ID_KEY);

                // Validate that all required data is present
                if (string.IsNullOrEmpty(authToken) ||
                    string.IsNullOrEmpty(studentDataJson) ||
                    string.IsNullOrEmpty(tokenExpiryString) ||
                    string.IsNullOrEmpty(userId))
                {
                    return false;
                }

                // Parse token expiry
                if (!DateTime.TryParse(tokenExpiryString, out var tokenExpiry))
                {
                    await ClearAuthenticationDataAsync();
                    return false;
                }

                // Check if token has expired
                if (DateTime.Now >= tokenExpiry)
                {
                    // Try to refresh the token
                    var refreshSuccess = await TryRefreshTokenAsync();
                    if (!refreshSuccess)
                    {
                        await ClearAuthenticationDataAsync();
                        return false;
                    }
                    return true;
                }

                // Verify user still exists and is active in Supabase
                var userEntity = await _supabaseClient
                    .From<UserEntity>()
                    .Where(x => x.Id == userId && x.IsActive)
                    .Single();

                if (userEntity == null)
                {
                    await ClearAuthenticationDataAsync();
                    return false;
                }

                // Deserialize student data
                var student = JsonSerializer.Deserialize<Student>(studentDataJson);
                if (student == null)
                {
                    await ClearAuthenticationDataAsync();
                    return false;
                }

                // Restore authentication state
                _authToken = authToken;
                _currentStudent = student;
                _tokenExpiry = tokenExpiry;
                _refreshToken = await SecureStorage.GetAsync(REFRESH_TOKEN_KEY);

                // Notify authentication state change
                OnAuthenticationStateChanged(true, _currentStudent);

                return true;
            }
            catch (Exception ex)
            {
                // Log error and clear any corrupted data
                System.Diagnostics.Debug.WriteLine($"Restore authentication error: {ex.Message}");
                await ClearAuthenticationDataAsync();
                return false;
            }
        }

        /// <summary>
        /// Refresh the authentication token if it's about to expire
        /// </summary>
        /// <returns>True if token was refreshed successfully</returns>
        public async Task<bool> TryRefreshTokenAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_refreshToken))
                    return false;

                // In a real implementation, this would call an API endpoint
                // For now, we'll generate a new token
                _authToken = GenerateAuthToken();
                _tokenExpiry = DateTime.Now.AddHours(24);

                // Update stored token
                await SecureStorage.SetAsync(AUTH_TOKEN_KEY, _authToken);
                await SecureStorage.SetAsync(TOKEN_EXPIRY_KEY, _tokenExpiry.ToString());

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Token refresh error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region User Management

        /// <summary>
        /// Update current student information
        /// </summary>
        /// <param name="updatedStudent">Updated student data</param>
        public async Task UpdateStudentInfoAsync(Student updatedStudent)
        {
            if (updatedStudent == null)
                throw new ArgumentNullException(nameof(updatedStudent));

            if (!IsAuthenticated)
                throw new InvalidOperationException("User must be authenticated to update student info");

            try
            {
                _currentStudent = updatedStudent;
                await SaveStudentDataAsync();

                // Notify of authentication state change (user data updated)
                OnAuthenticationStateChanged(true, _currentStudent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update student info error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        /// <param name="currentPassword">Current password</param>
        /// <param name="newPassword">New password</param>
        /// <returns>True if password was changed successfully</returns>
        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                if (!IsAuthenticated)
                    throw new InvalidOperationException("User must be authenticated to change password");

                if (string.IsNullOrWhiteSpace(currentPassword))
                    throw new ArgumentException("Current password is required", nameof(currentPassword));

                if (string.IsNullOrWhiteSpace(newPassword))
                    throw new ArgumentException("New password is required", nameof(newPassword));

                if (newPassword.Length < 5)
                    throw new ArgumentException("New password must be at least 5 characters long", nameof(newPassword));

                // Get current user ID from storage
                var userId = await SecureStorage.GetAsync(USER_ID_KEY);
                if (string.IsNullOrEmpty(userId))
                    return false;

                // Verify current password
                var userEntity = await _supabaseClient
                    .From<UserEntity>()
                    .Where(x => x.Id == userId && x.Password == currentPassword)
                    .Single();

                if (userEntity == null)
                    return false; // Current password is incorrect

                // Update password
                userEntity.Password = newPassword;
                await userEntity.Update<UserEntity>();

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Change password error: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Authorization

        /// <summary>
        /// Check if user has permission to access a specific resource
        /// </summary>
        /// <param name="permission">Permission to check</param>
        /// <returns>True if user has the permission</returns>
        public bool HasPermission(string permission)
        {
            if (!IsAuthenticated)
                return false;

            // For student app, most permissions are granted by default
            return permission switch
            {
                "view_assignments" => true,
                "submit_assignments" => true,
                "view_fees" => true,
                "make_payments" => true,
                "view_library" => true,
                "borrow_books" => true,
                "view_activities" => true,
                "join_activities" => true,
                "admin_access" => _currentStudent?.Form?.Contains("Principal") == true || _currentStudent?.Form?.Contains("Secretary") == true,
                _ => true
            };
        }

        /// <summary>
        /// Get authentication header for API requests
        /// </summary>
        /// <returns>Authorization header value</returns>
        public string GetAuthorizationHeader()
        {
            if (!IsAuthenticated)
                throw new InvalidOperationException("User is not authenticated");

            return $"Bearer {_authToken}";
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Save authentication data to secure storage
        /// </summary>
        private async Task SaveAuthenticationDataAsync()
        {
            try
            {
                await SecureStorage.SetAsync(AUTH_TOKEN_KEY, _authToken ?? "");
                await SecureStorage.SetAsync(REFRESH_TOKEN_KEY, _refreshToken ?? "");
                await SecureStorage.SetAsync(TOKEN_EXPIRY_KEY, _tokenExpiry.ToString());

                // Store user ID for verification
                var userId = await SecureStorage.GetAsync(USER_ID_KEY);
                if (string.IsNullOrEmpty(userId) && _currentStudent != null)
                {
                    // Get user ID from Supabase based on phone number
                    var userEntity = await _supabaseClient
                        .From<UserEntity>()
                        .Where(x => x.PhoneNumber == NormalizePhoneNumber(_currentStudent.PhoneNumber))
                        .Single();

                    if (userEntity != null)
                    {
                        await SecureStorage.SetAsync(USER_ID_KEY, userEntity.Id);
                        await SecureStorage.SetAsync(USER_TYPE_KEY, userEntity.UserType);
                    }
                }

                await SaveStudentDataAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save authentication data error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Save student data to secure storage
        /// </summary>
        private async Task SaveStudentDataAsync()
        {
            try
            {
                if (_currentStudent != null)
                {
                    var studentJson = JsonSerializer.Serialize(_currentStudent);
                    await SecureStorage.SetAsync(STUDENT_DATA_KEY, studentJson);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save student data error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Clear all authentication data from secure storage
        /// </summary>
        private async Task ClearAuthenticationDataAsync()
        {
            try
            {
                SecureStorage.Remove(AUTH_TOKEN_KEY);
                SecureStorage.Remove(REFRESH_TOKEN_KEY);
                SecureStorage.Remove(STUDENT_DATA_KEY);
                SecureStorage.Remove(TOKEN_EXPIRY_KEY);
                SecureStorage.Remove(USER_ID_KEY);
                SecureStorage.Remove(USER_TYPE_KEY);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Clear authentication data error: {ex.Message}");
                // Don't throw - clearing should always succeed
            }
        }

        /// <summary>
        /// Normalize phone number to a consistent format
        /// </summary>
        /// <param name="phoneNumber">Input phone number</param>
        /// <returns>Normalized phone number</returns>
        private string NormalizePhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return phoneNumber;

            // Remove all non-digit characters except +
            var cleaned = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // Handle different formats
            if (cleaned.StartsWith("+254") && cleaned.Length == 13)
            {
                return cleaned; // +254XXXXXXXXX
            }
            else if (cleaned.StartsWith("254") && cleaned.Length == 12)
            {
                return "+" + cleaned; // Convert 254XXXXXXXXX to +254XXXXXXXXX
            }
            else if (cleaned.StartsWith("0") && cleaned.Length == 10)
            {
                return "+254" + cleaned.Substring(1); // Convert 0XXXXXXXXX to +254XXXXXXXXX
            }
            else if (cleaned.Length == 9 && !cleaned.StartsWith("0"))
            {
                return "+254" + cleaned; // Add +254 prefix
            }
            else
            {
                return phoneNumber; // Return as-is if format is unclear
            }
        }

        /// <summary>
        /// Generate authentication token
        /// </summary>
        private string GenerateAuthToken()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = Guid.NewGuid().ToString("N")[..8];
            return $"grs_token_{timestamp}_{random}";
        }

        /// <summary>
        /// Generate refresh token
        /// </summary>
        private string GenerateRefreshToken()
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var random = Guid.NewGuid().ToString("N")[..12];
            return $"grs_refresh_{timestamp}_{random}";
        }

        /// <summary>
        /// Notify subscribers of authentication state changes
        /// </summary>
        /// <param name="isAuthenticated">Whether user is authenticated</param>
        /// <param name="student">Current student (null if logged out)</param>
        private void OnAuthenticationStateChanged(bool isAuthenticated, Student student)
        {
            try
            {
                AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs
                {
                    IsAuthenticated = isAuthenticated,
                    Student = student,
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication state change notification error: {ex.Message}");
                // Don't throw - notification failures shouldn't break authentication
            }
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Get time until token expires
        /// </summary>
        /// <returns>TimeSpan until expiry, or TimeSpan.Zero if not authenticated</returns>
        public TimeSpan GetTimeUntilTokenExpiry()
        {
            if (!IsAuthenticated)
                return TimeSpan.Zero;

            var timeUntilExpiry = _tokenExpiry - DateTime.Now;
            return timeUntilExpiry > TimeSpan.Zero ? timeUntilExpiry : TimeSpan.Zero;
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Dispose of resources
        /// </summary>
        public void Dispose()
        {
            // Clean up any resources if needed
        }

        #endregion
    }

    #region Event Args

    /// <summary>
    /// Event arguments for authentication state changes
    /// </summary>
    public class AuthenticationStateChangedEventArgs : EventArgs
    {
        public bool IsAuthenticated { get; set; }
        public Student Student { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}