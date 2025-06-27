// ===============================
// Services/AuthService.cs - Fixed with Supabase Integration
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
        private Teacher _currentTeacher;
        private Staff _currentStaff;
        private string _authToken;
        private string _refreshToken;
        private DateTime _tokenExpiry;
        private string _currentUserType;

        // Events for authentication state changes
        public event EventHandler<AuthenticationStateChangedEventArgs> AuthenticationStateChanged;

        // Constants for secure storage keys
        private const string AUTH_TOKEN_KEY = "auth_token";
        private const string REFRESH_TOKEN_KEY = "refresh_token";
        private const string STUDENT_DATA_KEY = "student_data";
        private const string TEACHER_DATA_KEY = "teacher_data";
        private const string STAFF_DATA_KEY = "staff_data";
        private const string TOKEN_EXPIRY_KEY = "token_expiry";
        private const string USER_ID_KEY = "user_id";
        private const string USER_TYPE_KEY = "user_type";

        public AuthService()
        {
            _supabaseClient = SupabaseService.Instance.Client;
        }

        #region Public Properties

        /// <summary>
        /// Gets the currently authenticated student
        /// </summary>
        public Student CurrentStudent => _currentStudent;

        /// <summary>
        /// Gets the currently authenticated teacher
        /// </summary>
        public Teacher CurrentTeacher => _currentTeacher;

        /// <summary>
        /// Gets the currently authenticated staff
        /// </summary>
        public Staff CurrentStaff => _currentStaff;

        /// <summary>
        /// Gets the current authentication token
        /// </summary>
        public string AuthToken => _authToken;

        /// <summary>
        /// Gets the current user type
        /// </summary>
        public string CurrentUserType => _currentUserType;

        /// <summary>
        /// Gets whether the user is currently authenticated
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_authToken) &&
                                      (_currentStudent != null || _currentTeacher != null || _currentStaff != null) &&
                                      DateTime.Now < _tokenExpiry;

        /// <summary>
        /// Gets whether the token is about to expire (within 5 minutes)
        /// </summary>
        public bool IsTokenExpiringSoon => IsAuthenticated &&
                                          DateTime.Now.AddMinutes(5) >= _tokenExpiry;

        /// <summary>
        /// Gets the current user's role/permissions
        /// </summary>
        public string UserRole => _currentUserType switch
        {
            "Student" => _currentStudent?.Form ?? "Student",
            "Teacher" => "Teacher",
            _ => _currentStaff?.Position ?? "Staff"
        };

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

                // Set current user type
                _currentUserType = userResponse.UserType;

                // Get user profile based on user type
                var loginResponse = new LoginResponse
                {
                    Success = true,
                    Token = GenerateAuthToken(),
                    UserType = userResponse.UserType,
                    UserId = userResponse.Id,
                    Message = "Login successful"
                };

                switch (userResponse.UserType)
                {
                    case "Student":
                        var student = await GetStudentProfileAsync(userResponse);
                        if (student == null)
                        {
                            return new LoginResponse
                            {
                                Success = false,
                                Message = "Student profile not found",
                                ErrorCode = "PROFILE_NOT_FOUND"
                            };
                        }
                        _currentStudent = student;
                        loginResponse.Student = student;
                        break;

                    case "Teacher":
                        var teacher = await GetTeacherProfileAsync(userResponse);
                        if (teacher == null)
                        {
                            return new LoginResponse
                            {
                                Success = false,
                                Message = "Teacher profile not found",
                                ErrorCode = "PROFILE_NOT_FOUND"
                            };
                        }
                        _currentTeacher = teacher;
                        loginResponse.Teacher = teacher;
                        break;

                    default:
                        var staff = await GetStaffProfileAsync(userResponse);
                        if (staff == null)
                        {
                            return new LoginResponse
                            {
                                Success = false,
                                Message = "Staff profile not found",
                                ErrorCode = "PROFILE_NOT_FOUND"
                            };
                        }
                        _currentStaff = staff;
                        loginResponse.Staff = staff;
                        break;
                }

                // Store authentication data
                _authToken = loginResponse.Token;
                _refreshToken = GenerateRefreshToken();
                _tokenExpiry = DateTime.Now.AddHours(24); // Token valid for 24 hours

                // Persist authentication data securely
                await SaveAuthenticationDataAsync(userResponse.Id, userResponse.UserType);

                // Notify authentication state change
                OnAuthenticationStateChanged(true, _currentStudent, _currentTeacher, _currentStaff);

                return loginResponse;
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
                var studentEntity = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.UserId == userEntity.Id)
                    .Single();

                if (studentEntity != null)
                {
                    return new Student
                    {
                        StudentId = studentEntity.StudentId,
                        FullName = studentEntity.FullName,
                        AdmissionNo = studentEntity.AdmissionNo,
                        Form = studentEntity.Form,
                        Class = studentEntity.Class,
                        Email = studentEntity.Email,
                        PhoneNumber = userEntity.PhoneNumber,
                        ParentPhone = studentEntity.ParentPhone,
                        DateOfBirth = studentEntity.DateOfBirth,
                        Gender = studentEntity.Gender,
                        Address = studentEntity.Address
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get student profile error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get teacher profile from Supabase based on user entity
        /// </summary>
        private async Task<Teacher> GetTeacherProfileAsync(UserEntity userEntity)
        {
            try
            {
                var teacherEntity = await _supabaseClient
                    .From<TeacherEntity>()
                    .Where(x => x.UserId == userEntity.Id)
                    .Single();

                if (teacherEntity != null)
                {
                    return new Teacher
                    {
                        TeacherId = teacherEntity.TeacherId,
                        FullName = teacherEntity.FullName,
                        Email = teacherEntity.Email,
                        PhoneNumber = teacherEntity.PhoneNumber,
                        EmployeeNumber = teacherEntity.EmployeeNumber,
                        Department = teacherEntity.Department,
                        QualifiedSubjects = teacherEntity.QualifiedSubjects ?? new List<string>(),
                        DateJoined = teacherEntity.DateJoined
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get teacher profile error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get staff profile from Supabase based on user entity
        /// </summary>
        private async Task<Staff> GetStaffProfileAsync(UserEntity userEntity)
        {
            try
            {
                var staffEntity = await _supabaseClient
                    .From<StaffEntity>()
                    .Where(x => x.UserId == userEntity.Id)
                    .Single();

                if (staffEntity != null)
                {
                    return new Staff
                    {
                        StaffId = staffEntity.StaffId,
                        FullName = staffEntity.FullName,
                        Email = staffEntity.Email,
                        PhoneNumber = staffEntity.PhoneNumber,
                        EmployeeNumber = staffEntity.EmployeeNumber,
                        Department = staffEntity.Department,
                        Position = staffEntity.Position,
                        DateJoined = staffEntity.DateJoined
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get staff profile error: {ex.Message}");
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
                var previousTeacher = _currentTeacher;
                var previousStaff = _currentStaff;

                // Clear in-memory data
                _currentStudent = null;
                _currentTeacher = null;
                _currentStaff = null;
                _authToken = null;
                _refreshToken = null;
                _tokenExpiry = DateTime.MinValue;
                _currentUserType = null;

                // Clear persisted data
                await ClearAuthenticationDataAsync();

                // Notify authentication state change
                if (wasAuthenticated)
                {
                    OnAuthenticationStateChanged(false, previousStudent, previousTeacher, previousStaff);
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
                var tokenExpiryString = await SecureStorage.GetAsync(TOKEN_EXPIRY_KEY);
                var userId = await SecureStorage.GetAsync(USER_ID_KEY);
                var userType = await SecureStorage.GetAsync(USER_TYPE_KEY);

                // Validate that all required data is present
                if (string.IsNullOrEmpty(authToken) ||
                    string.IsNullOrEmpty(tokenExpiryString) ||
                    string.IsNullOrEmpty(userId) ||
                    string.IsNullOrEmpty(userType))
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

                // Restore user profile based on type
                _currentUserType = userType;

                switch (userType)
                {
                    case "Student":
                        var studentDataJson = await SecureStorage.GetAsync(STUDENT_DATA_KEY);
                        if (!string.IsNullOrEmpty(studentDataJson))
                        {
                            _currentStudent = JsonSerializer.Deserialize<Student>(studentDataJson);
                        }
                        break;

                    case "Teacher":
                        var teacherDataJson = await SecureStorage.GetAsync(TEACHER_DATA_KEY);
                        if (!string.IsNullOrEmpty(teacherDataJson))
                        {
                            _currentTeacher = JsonSerializer.Deserialize<Teacher>(teacherDataJson);
                        }
                        break;

                    default:
                        var staffDataJson = await SecureStorage.GetAsync(STAFF_DATA_KEY);
                        if (!string.IsNullOrEmpty(staffDataJson))
                        {
                            _currentStaff = JsonSerializer.Deserialize<Staff>(staffDataJson);
                        }
                        break;
                }

                // Restore authentication state
                _authToken = authToken;
                _tokenExpiry = tokenExpiry;
                _refreshToken = await SecureStorage.GetAsync(REFRESH_TOKEN_KEY);

                // Notify authentication state change
                OnAuthenticationStateChanged(true, _currentStudent, _currentTeacher, _currentStaff);

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

        #region Private Methods

        /// <summary>
        /// Save authentication data to secure storage
        /// </summary>
        private async Task SaveAuthenticationDataAsync(string userId, string userType)
        {
            try
            {
                await SecureStorage.SetAsync(AUTH_TOKEN_KEY, _authToken ?? "");
                await SecureStorage.SetAsync(REFRESH_TOKEN_KEY, _refreshToken ?? "");
                await SecureStorage.SetAsync(TOKEN_EXPIRY_KEY, _tokenExpiry.ToString());
                await SecureStorage.SetAsync(USER_ID_KEY, userId);
                await SecureStorage.SetAsync(USER_TYPE_KEY, userType);

                // Save user data based on type
                switch (userType)
                {
                    case "Student" when _currentStudent != null:
                        var studentJson = JsonSerializer.Serialize(_currentStudent);
                        await SecureStorage.SetAsync(STUDENT_DATA_KEY, studentJson);
                        break;

                    case "Teacher" when _currentTeacher != null:
                        var teacherJson = JsonSerializer.Serialize(_currentTeacher);
                        await SecureStorage.SetAsync(TEACHER_DATA_KEY, teacherJson);
                        break;

                    default:
                        if (_currentStaff != null)
                        {
                            var staffJson = JsonSerializer.Serialize(_currentStaff);
                            await SecureStorage.SetAsync(STAFF_DATA_KEY, staffJson);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Save authentication data error: {ex.Message}");
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
                SecureStorage.Remove(TEACHER_DATA_KEY);
                SecureStorage.Remove(STAFF_DATA_KEY);
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
        private void OnAuthenticationStateChanged(bool isAuthenticated, Student student, Teacher teacher, Staff staff)
        {
            try
            {
                AuthenticationStateChanged?.Invoke(this, new AuthenticationStateChangedEventArgs
                {
                    IsAuthenticated = isAuthenticated,
                    Student = student,
                    Teacher = teacher,
                    Staff = staff,
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

            return permission switch
            {
                "view_assignments" => true,
                "submit_assignments" => _currentUserType == "Student",
                "create_assignments" => _currentUserType == "Teacher",
                "view_fees" => true,
                "make_payments" => _currentUserType == "Student",
                "view_library" => true,
                "borrow_books" => _currentUserType == "Student",
                "view_activities" => true,
                "join_activities" => _currentUserType == "Student",
                "admin_access" => _currentUserType != "Student",
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
        public Teacher Teacher { get; set; }
        public Staff Staff { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}