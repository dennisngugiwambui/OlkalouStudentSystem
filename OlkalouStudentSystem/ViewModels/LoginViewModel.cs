// ===============================
// ViewModels/LoginViewModel.cs - Database Connected Login ViewModel
// ===============================
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Services;
using OlkalouStudentSystem.Models.Data;

namespace OlkalouStudentSystem.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private readonly AuthService _authService;
        private readonly Supabase.Client _supabaseClient;

        #region Private Fields
        private string _phoneNumber = string.Empty;
        private string _phoneNumberDisplay = string.Empty;
        private string _password = string.Empty;
        private string _phoneErrorMessage = string.Empty;
        private string _passwordErrorMessage = string.Empty;
        private bool _isBusy = false;
        private bool _isLoginEnabled = true;
        private string _loadingDots = "";
        private System.Timers.Timer _loadingTimer;
        #endregion

        #region Public Properties
        public string PhoneNumber
        {
            get => _phoneNumber;
            set
            {
                _phoneNumber = value;
                OnPropertyChanged();
                ValidateForm();
            }
        }

        public string PhoneNumberDisplay
        {
            get => _phoneNumberDisplay;
            set
            {
                _phoneNumberDisplay = value;
                OnPropertyChanged();
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                _password = value;
                OnPropertyChanged();
                ValidateForm();
                // Clear password error when user starts typing
                if (!string.IsNullOrEmpty(value))
                {
                    PasswordErrorMessage = "";
                }
            }
        }

        public string PhoneErrorMessage
        {
            get => _phoneErrorMessage;
            set
            {
                _phoneErrorMessage = value;
                OnPropertyChanged();
            }
        }

        public string PasswordErrorMessage
        {
            get => _passwordErrorMessage;
            set
            {
                _passwordErrorMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                IsLoginEnabled = !value;
            }
        }

        public bool IsLoginEnabled
        {
            get => _isLoginEnabled;
            set
            {
                _isLoginEnabled = value;
                OnPropertyChanged();
            }
        }

        public string LoadingDots
        {
            get => _loadingDots;
            set
            {
                _loadingDots = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }
        #endregion

        #region Constructor
        public LoginViewModel(AuthService authService)
        {
            _authService = authService;
            _supabaseClient = SupabaseService.Instance;
            LoginCommand = new Command(async () => await ExecuteLoginAsync(), () => CanExecuteLogin());

            // Initialize loading animation timer
            _loadingTimer = new System.Timers.Timer(500);
            _loadingTimer.Elapsed += OnLoadingTimerElapsed;
        }
        #endregion

        #region Commands
        private bool CanExecuteLogin()
        {
            return IsLoginEnabled && 
                   !string.IsNullOrWhiteSpace(PhoneNumber) && 
                   !string.IsNullOrWhiteSpace(Password) &&
                   string.IsNullOrEmpty(PhoneErrorMessage) &&
                   string.IsNullOrEmpty(PasswordErrorMessage);
        }

        private async Task ExecuteLoginAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                StartLoadingAnimation();

                // Validate inputs
                if (!ValidateInputs())
                {
                    return;
                }

                // Attempt login
                var loginResult = await AuthenticateUserAsync();

                if (loginResult.Success)
                {
                    // Store user data in secure storage
                    await StoreUserDataAsync(loginResult);

                    // Navigate based on user type
                    await NavigateToMainPageAsync(loginResult.UserType);
                }
                else
                {
                    await ShowErrorAsync("Login Failed", loginResult.Message);
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"An unexpected error occurred: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Login error: {ex}");
            }
            finally
            {
                IsBusy = false;
                StopLoadingAnimation();
            }
        }
        #endregion

        #region Authentication Methods
        private async Task<LoginResult> AuthenticateUserAsync()
        {
            try
            {
                // Query the users table for authentication
                var user = await _supabaseClient
                    .From<UserEntity>()
                    .Where(x => x.PhoneNumber == PhoneNumber && x.IsActive)
                    .Single();

                if (user == null)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Phone number not found. Please check your credentials."
                    };
                }

                // Verify password (in production, use proper password hashing)
                if (user.Password != Password)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "Invalid password. Please try again."
                    };
                }

                // Get user profile based on user type
                var userProfile = await GetUserProfileAsync(user);

                if (userProfile == null)
                {
                    return new LoginResult
                    {
                        Success = false,
                        Message = "User profile not found. Please contact administrator."
                    };
                }

                // Update last login
                user.LastLogin = DateTime.UtcNow;
                await user.Update<UserEntity>();

                return new LoginResult
                {
                    Success = true,
                    Message = "Login successful",
                    UserId = user.Id,
                    UserType = user.UserType,
                    UserProfile = userProfile
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Authentication error: {ex.Message}");
                return new LoginResult
                {
                    Success = false,
                    Message = "Unable to connect to server. Please try again."
                };
            }
        }

        private async Task<object?> GetUserProfileAsync(UserEntity user)
        {
            try
            {
                return user.UserType switch
                {
                    "Student" => await _supabaseClient
                        .From<StudentEntity>()
                        .Where(x => x.UserId == user.Id)
                        .Single(),
                    
                    "Teacher" => await _supabaseClient
                        .From<TeacherEntity>()
                        .Where(x => x.UserId == user.Id)
                        .Single(),
                    
                    "Principal" or "DeputyPrincipal" or "Secretary" or "Bursar" => await _supabaseClient
                        .From<StaffEntity>()
                        .Where(x => x.UserId == user.Id)
                        .Single(),
                    
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private async Task StoreUserDataAsync(LoginResult loginResult)
        {
            try
            {
                await SecureStorage.SetAsync("user_id", loginResult.UserId);
                await SecureStorage.SetAsync("user_type", loginResult.UserType);
                await SecureStorage.SetAsync("is_logged_in", "true");

                // Store profile-specific data
                switch (loginResult.UserProfile)
                {
                    case StudentEntity student:
                        await SecureStorage.SetAsync("student_id", student.StudentId);
                        await SecureStorage.SetAsync("full_name", student.FullName);
                        await SecureStorage.SetAsync("form", student.Form);
                        await SecureStorage.SetAsync("class", student.Class);
                        break;

                    case TeacherEntity teacher:
                        await SecureStorage.SetAsync("teacher_id", teacher.TeacherId);
                        await SecureStorage.SetAsync("full_name", teacher.FullName);
                        await SecureStorage.SetAsync("employee_type", teacher.EmployeeType);
                        break;

                    case StaffEntity staff:
                        await SecureStorage.SetAsync("staff_id", staff.StaffId);
                        await SecureStorage.SetAsync("full_name", staff.FullName);
                        await SecureStorage.SetAsync("position", staff.Position);
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error storing user data: {ex.Message}");
            }
        }

        private async Task NavigateToMainPageAsync(string userType)
        {
            try
            {
                // Navigate based on user type
                var route = userType switch
                {
                    "Student" => "//student",
                    "Teacher" => "//teacher", 
                    "Principal" or "DeputyPrincipal" => "//admin",
                    "Secretary" => "//secretary",
                    "Bursar" => "//bursar",
                    _ => "//main"
                };

                await Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex.Message}");
                // Fallback navigation
                await Shell.Current.GoToAsync("//main");
            }
        }
        #endregion

        #region Validation Methods
        private bool ValidateInputs()
        {
            bool isValid = true;

            // Validate phone number
            if (string.IsNullOrWhiteSpace(PhoneNumberDisplay))
            {
                PhoneErrorMessage = "Phone number is required";
                isValid = false;
            }
            else if (PhoneNumberDisplay.Length != 9)
            {
                PhoneErrorMessage = "Phone number must be 9 digits";
                isValid = false;
            }
            else if (!PhoneNumberDisplay.All(char.IsDigit))
            {
                PhoneErrorMessage = "Phone number must contain only digits";
                isValid = false;
            }
            else
            {
                PhoneErrorMessage = "";
            }

            // Validate password
            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordErrorMessage = "Password is required";
                isValid = false;
            }
            else if (Password.Length < 4)
            {
                PasswordErrorMessage = "Password must be at least 4 characters";
                isValid = false;
            }
            else
            {
                PasswordErrorMessage = "";
            }

            return isValid;
        }

        private void ValidateForm()
        {
            // Update login button state
            ((Command)LoginCommand).ChangeCanExecute();
        }
        #endregion

        #region Helper Methods
        private void StartLoadingAnimation()
        {
            LoadingDots = "";
            _loadingTimer.Start();
        }

        private void StopLoadingAnimation()
        {
            _loadingTimer.Stop();
            LoadingDots = "";
        }

        private void OnLoadingTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var currentDots = LoadingDots.Length;
            LoadingDots = currentDots switch
            {
                0 => ".",
                1 => "..",
                2 => "...",
                _ => ""
            };
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            try
            {
                await Application.Current.MainPage.DisplayAlert(title, message, "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing alert: {ex.Message}");
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            _loadingTimer?.Dispose();
        }
        #endregion
    }

    #region Supporting Models
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public object? UserProfile { get; set; }
    }
    #endregion
}