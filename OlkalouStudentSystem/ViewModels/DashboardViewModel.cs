// ===============================
// ViewModels/DashboardViewModel.cs - Enhanced Dashboard ViewModel with Role-based Features
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace OlkalouStudentSystem.ViewModels
{
    /// <summary>
    /// Enhanced Dashboard ViewModel with comprehensive role-based functionality
    /// </summary>
    public class DashboardViewModel : INotifyPropertyChanged
    {
        #region Fields
        private readonly AuthService _authService;
        private readonly FeesService _feesService;
        private readonly ApiService _apiService;
        private readonly ILogger<DashboardViewModel>? _logger;

        private bool _isBusy;
        private bool _isRefreshing;
        private bool _isMenuVisible;
        private string _welcomeMessage = "Welcome";
        private string _userType = "Student";
        private string _userName = "";
        private string _userRole = "";
        private DashboardData? _dashboardData;

        // Student-specific properties
        private FeesInfo? _currentFees;
        private ObservableCollection<Assignment> _pendingAssignments = new();
        private ObservableCollection<LibraryBook> _issuedBooks = new();
        private ObservableCollection<Achievement> _recentAchievements = new();
        private ObservableCollection<Activity> _upcomingActivities = new();
        private ObservableCollection<Models.SubjectPerformance> _academicPerformance = new();

        // Teacher-specific properties
        private ObservableCollection<TeacherClass> _teacherClasses = new();
        private ObservableCollection<string> _teacherSubjects = new();
        private ObservableCollection<PendingMark> _pendingMarks = new();

        // Admin/Principal-specific properties
        private SchoolStatistics? _schoolStatistics;
        private ObservableCollection<AdminActivity> _recentActivities = new();

        // Secretary-specific properties
        private RegistrationStatistics? _registrationStatistics;

        // Bursar-specific properties
        private FinancialStatistics? _financialStatistics;
        private ObservableCollection<Models.Invoice> _pendingInvoices = new();
        private ObservableCollection<Models.SalaryPayment> _pendingSalaries = new();

        // General staff properties
        private BasicStatistics? _basicStatistics;

        // Dashboard tab properties
        private ObservableCollection<DashboardTab> _dashboardTabs = new();
        private DashboardTab? _selectedTab;
        #endregion

        #region Properties
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public bool IsMenuVisible
        {
            get => _isMenuVisible;
            set => SetProperty(ref _isMenuVisible, value);
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public string UserType
        {
            get => _userType;
            set => SetProperty(ref _userType, value);
        }

        public string UserName
        {
            get => _userName;
            set => SetProperty(ref _userName, value);
        }

        public string UserRole
        {
            get => _userRole;
            set => SetProperty(ref _userRole, value);
        }

        public DashboardData? DashboardData
        {
            get => _dashboardData;
            set => SetProperty(ref _dashboardData, value);
        }

        #region Student Properties
        public FeesInfo? CurrentFees
        {
            get => _currentFees;
            set => SetProperty(ref _currentFees, value);
        }

        public ObservableCollection<Assignment> PendingAssignments
        {
            get => _pendingAssignments;
            set => SetProperty(ref _pendingAssignments, value);
        }

        public ObservableCollection<LibraryBook> IssuedBooks
        {
            get => _issuedBooks;
            set => SetProperty(ref _issuedBooks, value);
        }

        public ObservableCollection<Achievement> RecentAchievements
        {
            get => _recentAchievements;
            set => SetProperty(ref _recentAchievements, value);
        }

        public ObservableCollection<Activity> UpcomingActivities
        {
            get => _upcomingActivities;
            set => SetProperty(ref _upcomingActivities, value);
        }

        public ObservableCollection<Models.SubjectPerformance> AcademicPerformance
        {
            get => _academicPerformance;
            set => SetProperty(ref _academicPerformance, value);
        }
        #endregion

        #region Teacher Properties
        public ObservableCollection<TeacherClass> TeacherClasses
        {
            get => _teacherClasses;
            set => SetProperty(ref _teacherClasses, value);
        }

        public ObservableCollection<string> TeacherSubjects
        {
            get => _teacherSubjects;
            set => SetProperty(ref _teacherSubjects, value);
        }

        public ObservableCollection<PendingMark> PendingMarks
        {
            get => _pendingMarks;
            set => SetProperty(ref _pendingMarks, value);
        }
        #endregion

        #region Admin Properties
        public SchoolStatistics? SchoolStatistics
        {
            get => _schoolStatistics;
            set => SetProperty(ref _schoolStatistics, value);
        }

        public ObservableCollection<AdminActivity> RecentActivities
        {
            get => _recentActivities;
            set => SetProperty(ref _recentActivities, value);
        }
        #endregion

        #region Secretary Properties
        public RegistrationStatistics? RegistrationStatistics
        {
            get => _registrationStatistics;
            set => SetProperty(ref _registrationStatistics, value);
        }
        #endregion

        #region Bursar Properties
        public FinancialStatistics? FinancialStatistics
        {
            get => _financialStatistics;
            set => SetProperty(ref _financialStatistics, value);
        }

        public ObservableCollection<Models.Invoice> PendingInvoices
        {
            get => _pendingInvoices;
            set => SetProperty(ref _pendingInvoices, value);
        }

        public ObservableCollection<Models.SalaryPayment> PendingSalaries
        {
            get => _pendingSalaries;
            set => SetProperty(ref _pendingSalaries, value);
        }
        #endregion

        #region General Staff Properties
        public BasicStatistics? BasicStatistics
        {
            get => _basicStatistics;
            set => SetProperty(ref _basicStatistics, value);
        }
        #endregion

        #region Dashboard Tab Properties
        public ObservableCollection<DashboardTab> DashboardTabs
        {
            get => _dashboardTabs;
            set => SetProperty(ref _dashboardTabs, value);
        }

        public DashboardTab? SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }
        #endregion
        #endregion

        #region Commands
        public ICommand RefreshCommand { get; }
        public ICommand ToggleMenuCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand ViewAssignmentCommand { get; }
        public ICommand ViewFeesCommand { get; }
        public ICommand ViewLibraryCommand { get; }
        public ICommand ViewActivitiesCommand { get; }
        public ICommand LogoutCommand { get; }
        #endregion

        #region Constructor
        public DashboardViewModel(AuthService authService, FeesService feesService, ApiService apiService, ILogger<DashboardViewModel>? logger = null)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _feesService = feesService ?? throw new ArgumentNullException(nameof(feesService));
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _logger = logger;

            // Initialize commands
            RefreshCommand = new Command(async () => await RefreshAsync(), () => !IsBusy);
            ToggleMenuCommand = new Command(() => IsMenuVisible = !IsMenuVisible);
            NavigateCommand = new Command<string>(async (page) => await NavigateToPageAsync(page));
            ViewAssignmentCommand = new Command<Assignment>(async (assignment) => await ViewAssignmentAsync(assignment));
            ViewFeesCommand = new Command(async () => await ViewFeesAsync());
            ViewLibraryCommand = new Command(async () => await ViewLibraryAsync());
            ViewActivitiesCommand = new Command(async () => await ViewActivitiesAsync());
            LogoutCommand = new Command(async () => await LogoutAsync());

            // Initialize user info
            InitializeUserInfo();
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Initialize the dashboard based on user type
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing dashboard for user type: {UserType}", UserType);

                IsBusy = true;

                // Initialize user information
                await InitializeUserInfoAsync();

                // Initialize dashboard tabs based on user type
                InitializeDashboardTabs();

                // Load role-specific data
                await LoadRoleSpecificDataAsync();

                _logger?.LogInformation("Dashboard initialized successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing dashboard");
                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Initialize user information from auth service
        /// </summary>
        private void InitializeUserInfo()
        {
            try
            {
                UserType = _authService.UserType?.ToString() ?? "Student";
                UserName = GetUserDisplayName();
                UserRole = GetUserRole();

                _logger?.LogInformation("User info initialized: {UserType} - {UserName}", UserType, UserName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing user info");
            }
        }

        /// <summary>
        /// Initialize user information asynchronously
        /// </summary>
        private async Task InitializeUserInfoAsync()
        {
            try
            {
                // Get current user type from storage
                var userType = await SecureStorage.GetAsync("user_type") ?? "Student";
                var userId = await SecureStorage.GetAsync("user_id");

                UserType = userType;

                // Get user details based on type
                switch (userType)
                {
                    case "Student":
                        if (_authService.CurrentStudent != null)
                        {
                            UserName = _authService.CurrentStudent.FullName;
                            UserRole = $"Form {_authService.CurrentStudent.Form} Student";
                        }
                        break;

                    case "Teacher":
                        if (_authService.CurrentTeacher != null)
                        {
                            UserName = _authService.CurrentTeacher.FullName;
                            UserRole = "Teacher";
                        }
                        break;

                    case "Principal":
                        UserRole = "Principal";
                        break;

                    case "Secretary":
                        UserRole = "Secretary";
                        break;

                    case "Bursar":
                        UserRole = "Bursar";
                        break;

                    default:
                        UserRole = "Staff Member";
                        break;
                }

                if (string.IsNullOrEmpty(UserName))
                {
                    UserName = UserRole;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing user info async");
            }
        }

        /// <summary>
        /// Initialize dashboard tabs based on user type
        /// </summary>
        private void InitializeDashboardTabs()
        {
            try
            {
                DashboardTabs.Clear();

                switch (UserType)
                {
                    case "Student":
                        InitializeStudentTabs();
                        break;

                    case "Teacher":
                        InitializeTeacherTabs();
                        break;

                    case "Principal":
                        InitializePrincipalTabs();
                        break;

                    case "Secretary":
                        InitializeSecretaryTabs();
                        break;

                    case "Bursar":
                        InitializeBursarTabs();
                        break;

                    default:
                        InitializeStaffTabs();
                        break;
                }

                // Select first tab by default
                if (DashboardTabs.Any())
                {
                    SelectedTab = DashboardTabs.First();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing dashboard tabs");
            }
        }

        /// <summary>
        /// Initialize tabs for student dashboard
        /// </summary>
        private void InitializeStudentTabs()
        {
            DashboardTabs.Add(new DashboardTab { Id = "overview", Title = "Overview", Icon = "home", IsSelected = true });
            DashboardTabs.Add(new DashboardTab { Id = "assignments", Title = "Assignments", Icon = "book" });
            DashboardTabs.Add(new DashboardTab { Id = "fees", Title = "Fees", Icon = "credit_card" });
            DashboardTabs.Add(new DashboardTab { Id = "library", Title = "Library", Icon = "library_books" });
            DashboardTabs.Add(new DashboardTab { Id = "activities", Title = "Activities", Icon = "event" });
            DashboardTabs.Add(new DashboardTab { Id = "performance", Title = "Performance", Icon = "trending_up" });
        }

        /// <summary>
        /// Initialize tabs for teacher dashboard
        /// </summary>
        private void InitializeTeacherTabs()
        {
            DashboardTabs.Add(new DashboardTab { Id = "overview", Title = "Overview", Icon = "dashboard", IsSelected = true });
            DashboardTabs.Add(new DashboardTab { Id = "classes", Title = "My Classes", Icon = "school" });
            DashboardTabs.Add(new DashboardTab { Id = "assignments", Title = "Assignments", Icon = "assignment" });
            DashboardTabs.Add(new DashboardTab { Id = "grading", Title = "Grading", Icon = "grade" });
            DashboardTabs.Add(new DashboardTab { Id = "students", Title = "Students", Icon = "people" });
            DashboardTabs.Add(new DashboardTab { Id = "schedule", Title = "Schedule", Icon = "schedule" });
        }

        /// <summary>
        /// Initialize tabs for principal dashboard
        /// </summary>
        private void InitializePrincipalTabs()
        {
            DashboardTabs.Add(new DashboardTab { Id = "overview", Title = "Overview", Icon = "dashboard", IsSelected = true });
            DashboardTabs.Add(new DashboardTab { Id = "statistics", Title = "Statistics", Icon = "analytics" });
            DashboardTabs.Add(new DashboardTab { Id = "staff", Title = "Staff", Icon = "people" });
            DashboardTabs.Add(new DashboardTab { Id = "students", Title = "Students", Icon = "school" });
            DashboardTabs.Add(new DashboardTab { Id = "finances", Title = "Finances", Icon = "attach_money" });
            DashboardTabs.Add(new DashboardTab { Id = "reports", Title = "Reports", Icon = "assessment" });
            DashboardTabs.Add(new DashboardTab { Id = "activities", Title = "Activities", Icon = "event" });
        }

        /// <summary>
        /// Initialize tabs for secretary dashboard
        /// </summary>
        private void InitializeSecretaryTabs()
        {
            DashboardTabs.Add(new DashboardTab { Id = "overview", Title = "Overview", Icon = "dashboard", IsSelected = true });
            DashboardTabs.Add(new DashboardTab { Id = "registrations", Title = "Registrations", Icon = "person_add" });
            DashboardTabs.Add(new DashboardTab { Id = "students", Title = "Students", Icon = "school" });
            DashboardTabs.Add(new DashboardTab { Id = "communications", Title = "Communications", Icon = "message" });
            DashboardTabs.Add(new DashboardTab { Id = "documents", Title = "Documents", Icon = "folder" });
            DashboardTabs.Add(new DashboardTab { Id = "schedule", Title = "Schedule", Icon = "calendar_today" });
        }

        /// <summary>
        /// Initialize tabs for bursar dashboard
        /// </summary>
        private void InitializeBursarTabs()
        {
            DashboardTabs.Add(new DashboardTab { Id = "overview", Title = "Overview", Icon = "dashboard", IsSelected = true });
            DashboardTabs.Add(new DashboardTab { Id = "fees", Title = "Fees Management", Icon = "payment" });
            DashboardTabs.Add(new DashboardTab { Id = "invoices", Title = "Invoices", Icon = "receipt" });
            DashboardTabs.Add(new DashboardTab { Id = "salaries", Title = "Salaries", Icon = "account_balance_wallet" });
            DashboardTabs.Add(new DashboardTab { Id = "reports", Title = "Financial Reports", Icon = "trending_up" });
            DashboardTabs.Add(new DashboardTab { Id = "accounts", Title = "Accounts", Icon = "account_balance" });
        }

        /// <summary>
        /// Initialize tabs for general staff dashboard
        /// </summary>
        private void InitializeStaffTabs()
        {
            DashboardTabs.Add(new DashboardTab { Id = "overview", Title = "Overview", Icon = "dashboard", IsSelected = true });
            DashboardTabs.Add(new DashboardTab { Id = "schedule", Title = "My Schedule", Icon = "schedule" });
            DashboardTabs.Add(new DashboardTab { Id = "tasks", Title = "Tasks", Icon = "task" });
            DashboardTabs.Add(new DashboardTab { Id = "communications", Title = "Communications", Icon = "message" });
        }

        /// <summary>
        /// Load role-specific data
        /// </summary>
        private async Task LoadRoleSpecificDataAsync()
        {
            try
            {
                switch (UserType)
                {
                    case "Student":
                        await LoadStudentDataAsync();
                        break;

                    case "Teacher":
                        await LoadTeacherDataAsync();
                        break;

                    case "Principal":
                        await LoadPrincipalDataAsync();
                        break;

                    case "Secretary":
                        await LoadSecretaryDataAsync();
                        break;

                    case "Bursar":
                        await LoadBursarDataAsync();
                        break;

                    default:
                        await LoadStaffDataAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading role-specific data for {UserType}", UserType);
                throw;
            }
        }
        #endregion

        #region Role-specific Data Loading

        /// <summary>
        /// Load student-specific dashboard data
        /// </summary>
        private async Task LoadStudentDataAsync()
        {
            try
            {
                var student = _authService.CurrentStudent;
                if (student == null) return;

                // Load fees information
                var feesResponse = await _apiService.GetFeesInfoAsync(student.StudentId);
                if (feesResponse.Success)
                {
                    CurrentFees = feesResponse.Data;
                }

                // Load pending assignments
                var assignmentsResponse = await _apiService.GetPendingAssignmentsAsync(student.StudentId);
                if (assignmentsResponse.Success)
                {
                    PendingAssignments = new ObservableCollection<Assignment>(assignmentsResponse.Data.Take(5));
                }

                // Load issued books
                var booksResponse = await _apiService.GetIssuedBooksAsync(student.StudentId);
                if (booksResponse.Success)
                {
                    // Convert BookIssue to LibraryBook for display
                    var libraryBooks = booksResponse.Data.Select(b => new LibraryBook
                    {
                        BookId = b.BookId,
                        Title = b.BookTitle,
                        Author = b.Author
                    }).Take(3);
                    IssuedBooks = new ObservableCollection<LibraryBook>(libraryBooks);
                }

                // Load achievements
                var achievementsResponse = await _apiService.GetStudentAchievementsAsync(student.StudentId);
                if (achievementsResponse.Success)
                {
                    RecentAchievements = new ObservableCollection<Achievement>(achievementsResponse.Data.Take(5));
                }

                // Load upcoming activities
                var activitiesResponse = await _apiService.GetActivitiesAsync(student.Form);
                if (activitiesResponse.Success)
                {
                    var upcoming = activitiesResponse.Data.Where(a => a.Date > DateTime.Now).Take(4);
                    UpcomingActivities = new ObservableCollection<Activity>(upcoming);
                }

                // Load academic performance
                var performanceResponse = await _apiService.GetStudentPerformanceAsync(student.StudentId);
                if (performanceResponse.Success)
                {
                    AcademicPerformance = new ObservableCollection<Models.SubjectPerformance>(performanceResponse.Data);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading student data");
                throw;
            }
        }

        /// <summary>
        /// Load teacher-specific dashboard data
        /// </summary>
        private async Task LoadTeacherDataAsync()
        {
            try
            {
                var teacher = _authService.CurrentTeacher;
                if (teacher == null) return;

                // Load teacher classes
                var classesResponse = await _apiService.GetTeacherClassesAsync(teacher.TeacherId);
                if (classesResponse.Success)
                {
                    TeacherClasses = new ObservableCollection<TeacherClass>(classesResponse.Data);
                }

                // Load teacher subjects
                var subjectsResponse = await _apiService.GetTeacherSubjectsAsync(teacher.TeacherId);
                if (subjectsResponse.Success)
                {
                    TeacherSubjects = new ObservableCollection<string>(subjectsResponse.Data);
                }

                // Load pending marks
                var marksResponse = await _apiService.GetPendingMarksAsync(teacher.TeacherId);
                if (marksResponse.Success)
                {
                    PendingMarks = new ObservableCollection<PendingMark>(marksResponse.Data);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading teacher data");
                throw;
            }
        }

        /// <summary>
        /// Load principal-specific dashboard data
        /// </summary>
        private async Task LoadPrincipalDataAsync()
        {
            try
            {
                // Load school statistics
                var statisticsResponse = await _apiService.GetSchoolStatisticsAsync();
                if (statisticsResponse.Success)
                {
                    SchoolStatistics = statisticsResponse.Data;
                }

                // Load recent activities
                var activitiesResponse = await _apiService.GetRecentActivitiesAsync();
                if (activitiesResponse.Success)
                {
                    RecentActivities = new ObservableCollection<AdminActivity>(activitiesResponse.Data);
                }

                // Load financial overview
                var financialResponse = await _apiService.GetFinancialStatisticsAsync();
                if (financialResponse.Success)
                {
                    FinancialStatistics = financialResponse.Data;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading principal data");
                throw;
            }
        }

        /// <summary>
        /// Load secretary-specific dashboard data
        /// </summary>
        private async Task LoadSecretaryDataAsync()
        {
            try
            {
                // Load registration statistics
                var registrationResponse = await _apiService.GetRegistrationStatisticsAsync();
                if (registrationResponse.Success)
                {
                    RegistrationStatistics = registrationResponse.Data;
                }

                // Load basic statistics
                var basicStatsResponse = await _apiService.GetBasicStatisticsAsync();
                if (basicStatsResponse.Success)
                {
                    BasicStatistics = basicStatsResponse.Data;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading secretary data");
                throw;
            }
        }

        /// <summary>
        /// Load bursar-specific dashboard data
        /// </summary>
        private async Task LoadBursarDataAsync()
        {
            try
            {
                // Load financial statistics
                var financialResponse = await _apiService.GetFinancialStatisticsAsync();
                if (financialResponse.Success)
                {
                    FinancialStatistics = financialResponse.Data;
                }

                // Load pending invoices
                var invoices = await _apiService.GetPendingInvoicesAsync();
                PendingInvoices = new ObservableCollection<Models.Invoice>(invoices);

                // Load pending salary payments
                var salaries = await _apiService.GetPendingSalaryPaymentsAsync();
                PendingSalaries = new ObservableCollection<Models.SalaryPayment>(salaries);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading bursar data");
                throw;
            }
        }

        /// <summary>
        /// Load general staff dashboard data
        /// </summary>
        private async Task LoadStaffDataAsync()
        {
            try
            {
                // Load basic statistics
                var basicStatsResponse = await _apiService.GetBasicStatisticsAsync();
                if (basicStatsResponse.Success)
                {
                    BasicStatistics = basicStatsResponse.Data;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading staff data");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get user display name based on user type
        /// </summary>
        private string GetUserDisplayName()
        {
            try
            {
                return UserType switch
                {
                    "Student" => _authService.CurrentStudent?.FullName ?? "Student",
                    "Teacher" => _authService.CurrentTeacher?.FullName ?? "Teacher",
                    _ => "User"
                };
            }
            catch
            {
                return "User";
            }
        }

        /// <summary>
        /// Get user role description
        /// </summary>
        private string GetUserRole()
        {
            try
            {
                return UserType switch
                {
                    "Student" => $"Form {_authService.CurrentStudent?.Form ?? "1"} Student",
                    "Teacher" => "Teacher",
                    "Principal" => "Principal",
                    "Secretary" => "Secretary",
                    "Bursar" => "Bursar",
                    _ => "Staff Member"
                };
            }
            catch
            {
                return "User";
            }
        }

        #endregion

        #region Command Implementations

        /// <summary>
        /// Refresh dashboard data
        /// </summary>
        private async Task RefreshAsync()
        {
            try
            {
                IsRefreshing = true;
                await LoadRoleSpecificDataAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing dashboard");
                // Show error message to user
                await Application.Current?.MainPage?.DisplayAlert("Error", "Failed to refresh dashboard data", "OK");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// Navigate to a specific page
        /// </summary>
        private async Task NavigateToPageAsync(string page)
        {
            try
            {
                if (string.IsNullOrEmpty(page)) return;

                await Shell.Current.GoToAsync($"//{page}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to page {Page}", page);
            }
        }

        /// <summary>
        /// View assignment details
        /// </summary>
        private async Task ViewAssignmentAsync(Assignment assignment)
        {
            try
            {
                if (assignment == null) return;

                await Shell.Current.GoToAsync($"//assignments?assignmentId={assignment.AssignmentId}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error viewing assignment {AssignmentId}", assignment?.AssignmentId);
            }
        }

        /// <summary>
        /// View fees information
        /// </summary>
        private async Task ViewFeesAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//fees");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to fees page");
            }
        }

        /// <summary>
        /// View library information
        /// </summary>
        private async Task ViewLibraryAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//library");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to library page");
            }
        }

        /// <summary>
        /// View activities
        /// </summary>
        private async Task ViewActivitiesAsync()
        {
            try
            {
                await Shell.Current.GoToAsync("//activities");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to activities page");
            }
        }

        /// <summary>
        /// Logout user
        /// </summary>
        private async Task LogoutAsync()
        {
            try
            {
                var result = await Application.Current?.MainPage?.DisplayAlert(
                    "Logout",
                    "Are you sure you want to logout?",
                    "Yes",
                    "No");

                if (result == true)
                {
                    await _authService.LogoutAsync();
                    await Shell.Current.GoToAsync("//login");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during logout");
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Dashboard tab model
    /// </summary>
    public class DashboardTab
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsSelected { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    #endregion
}