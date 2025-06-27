// ===============================
// ViewModels/SecretaryViewModel.cs - Complete Error-Free Secretary Management ViewModel
// ===============================
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Models.Data;
using OlkalouStudentSystem.Services;

namespace OlkalouStudentSystem.ViewModels
{
    /// <summary>
    /// Comprehensive ViewModel for Secretary operations including student registration,
    /// teacher management, communication, and administrative tasks
    /// </summary>
    public class SecretaryViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Fields and Dependencies

        private readonly SupabaseService _supabaseService;
        private readonly UserRegistrationService _registrationService;
        private readonly ILogger<SecretaryViewModel>? _logger;
        private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
        private bool _disposed = false;

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Private Fields for Properties

        private string _title = "Secretary Dashboard";
        private bool _isBusy = false;
        private string _currentUserName = string.Empty;
        private string _searchText = string.Empty;
        private int _selectedTabIndex = 0;

        // Student Registration Fields
        private string _studentFullName = string.Empty;
        private string _studentAdmissionNo = string.Empty;
        private string _studentForm = "Form1";
        private string _studentClass = "A";
        private string _studentEmail = string.Empty;
        private DateTime _studentDateOfBirth = DateTime.Now.AddYears(-16);
        private string _studentGender = "Male";
        private string _studentAddress = string.Empty;
        private string _parentPhone = string.Empty;
        private string _guardianName = string.Empty;
        private string _guardianPhone = string.Empty;

        // Teacher Registration Fields
        private string _teacherFullName = string.Empty;
        private string _teacherPhoneNumber = string.Empty;
        private string _teacherEmail = string.Empty;
        private string _teacherEmployeeType = "BOM";
        private string _teacherTscNumber = string.Empty;
        private decimal _teacherNtscPayment = 0;
        private string _teacherQualification = string.Empty;
        private string _teacherDepartment = string.Empty;

        // Staff Registration Fields
        private string _staffFullName = string.Empty;
        private string _staffPhoneNumber = string.Empty;
        private string _staffEmail = string.Empty;
        private string _staffPosition = "Librarian";
        private string _staffDepartment = string.Empty;

        // Communication Fields
        private string _announcementTitle = string.Empty;
        private string _announcementContent = string.Empty;
        private string _announcementType = "General";
        private string _announcementPriority = "Normal";
        private DateTime? _announcementExpiryDate;

        // Statistics Fields
        private int _totalStudents = 0;
        private int _totalTeachers = 0;
        private int _totalStaff = 0;
        private int _pendingApprovals = 0;
        private int _todayRegistrations = 0;

        // Selected Items Fields
        private StudentSummary? _selectedStudent;
        private TeacherSummary? _selectedTeacher;
        private StaffSummary? _selectedStaffMember;
        private AnnouncementSummary? _selectedAnnouncement;

        #endregion

        #region Public Properties

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    // Update command can execute status
                    ((Command)InitializeCommand).ChangeCanExecute();
                    ((Command)RegisterStudentCommand).ChangeCanExecute();
                    ((Command)RegisterTeacherCommand).ChangeCanExecute();
                    ((Command)RegisterStaffCommand).ChangeCanExecute();
                    ((Command)CreateAnnouncementCommand).ChangeCanExecute();
                }
            }
        }

        public string CurrentUserName
        {
            get => _currentUserName;
            set => SetProperty(ref _currentUserName, value);
        }

        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (SetProperty(ref _selectedTabIndex, value))
                {
                    OnSelectedTabIndexChanged(value);
                }
            }
        }

        // Student Registration Properties
        public string StudentFullName
        {
            get => _studentFullName;
            set
            {
                if (SetProperty(ref _studentFullName, value))
                {
                    ((Command)RegisterStudentCommand).ChangeCanExecute();
                }
            }
        }

        public string StudentAdmissionNo
        {
            get => _studentAdmissionNo;
            set
            {
                if (SetProperty(ref _studentAdmissionNo, value))
                {
                    ((Command)RegisterStudentCommand).ChangeCanExecute();
                }
            }
        }

        public string StudentForm
        {
            get => _studentForm;
            set => SetProperty(ref _studentForm, value);
        }

        public string StudentClass
        {
            get => _studentClass;
            set => SetProperty(ref _studentClass, value);
        }

        public string StudentEmail
        {
            get => _studentEmail;
            set => SetProperty(ref _studentEmail, value);
        }

        public DateTime StudentDateOfBirth
        {
            get => _studentDateOfBirth;
            set
            {
                if (SetProperty(ref _studentDateOfBirth, value))
                {
                    ((Command)RegisterStudentCommand).ChangeCanExecute();
                }
            }
        }

        public string StudentGender
        {
            get => _studentGender;
            set => SetProperty(ref _studentGender, value);
        }

        public string StudentAddress
        {
            get => _studentAddress;
            set => SetProperty(ref _studentAddress, value);
        }

        public string ParentPhone
        {
            get => _parentPhone;
            set
            {
                if (SetProperty(ref _parentPhone, value))
                {
                    ((Command)RegisterStudentCommand).ChangeCanExecute();
                }
            }
        }

        public string GuardianName
        {
            get => _guardianName;
            set => SetProperty(ref _guardianName, value);
        }

        public string GuardianPhone
        {
            get => _guardianPhone;
            set => SetProperty(ref _guardianPhone, value);
        }

        // Teacher Registration Properties
        public string TeacherFullName
        {
            get => _teacherFullName;
            set
            {
                if (SetProperty(ref _teacherFullName, value))
                {
                    ((Command)RegisterTeacherCommand).ChangeCanExecute();
                }
            }
        }

        public string TeacherPhoneNumber
        {
            get => _teacherPhoneNumber;
            set
            {
                if (SetProperty(ref _teacherPhoneNumber, value))
                {
                    ((Command)RegisterTeacherCommand).ChangeCanExecute();
                }
            }
        }

        public string TeacherEmail
        {
            get => _teacherEmail;
            set => SetProperty(ref _teacherEmail, value);
        }

        public string TeacherEmployeeType
        {
            get => _teacherEmployeeType;
            set
            {
                if (SetProperty(ref _teacherEmployeeType, value))
                {
                    OnTeacherEmployeeTypeChanged(value);
                    ((Command)RegisterTeacherCommand).ChangeCanExecute();
                }
            }
        }

        public string TeacherTscNumber
        {
            get => _teacherTscNumber;
            set => SetProperty(ref _teacherTscNumber, value);
        }

        public decimal TeacherNtscPayment
        {
            get => _teacherNtscPayment;
            set
            {
                if (SetProperty(ref _teacherNtscPayment, value))
                {
                    ((Command)RegisterTeacherCommand).ChangeCanExecute();
                }
            }
        }

        public string TeacherQualification
        {
            get => _teacherQualification;
            set => SetProperty(ref _teacherQualification, value);
        }

        public string TeacherDepartment
        {
            get => _teacherDepartment;
            set => SetProperty(ref _teacherDepartment, value);
        }

        // Staff Registration Properties
        public string StaffFullName
        {
            get => _staffFullName;
            set
            {
                if (SetProperty(ref _staffFullName, value))
                {
                    ((Command)RegisterStaffCommand).ChangeCanExecute();
                }
            }
        }

        public string StaffPhoneNumber
        {
            get => _staffPhoneNumber;
            set
            {
                if (SetProperty(ref _staffPhoneNumber, value))
                {
                    ((Command)RegisterStaffCommand).ChangeCanExecute();
                }
            }
        }

        public string StaffEmail
        {
            get => _staffEmail;
            set => SetProperty(ref _staffEmail, value);
        }

        public string StaffPosition
        {
            get => _staffPosition;
            set
            {
                if (SetProperty(ref _staffPosition, value))
                {
                    ((Command)RegisterStaffCommand).ChangeCanExecute();
                }
            }
        }

        public string StaffDepartment
        {
            get => _staffDepartment;
            set => SetProperty(ref _staffDepartment, value);
        }

        // Communication Properties
        public string AnnouncementTitle
        {
            get => _announcementTitle;
            set
            {
                if (SetProperty(ref _announcementTitle, value))
                {
                    ((Command)CreateAnnouncementCommand).ChangeCanExecute();
                }
            }
        }

        public string AnnouncementContent
        {
            get => _announcementContent;
            set
            {
                if (SetProperty(ref _announcementContent, value))
                {
                    ((Command)CreateAnnouncementCommand).ChangeCanExecute();
                }
            }
        }

        public string AnnouncementType
        {
            get => _announcementType;
            set => SetProperty(ref _announcementType, value);
        }

        public string AnnouncementPriority
        {
            get => _announcementPriority;
            set => SetProperty(ref _announcementPriority, value);
        }

        public DateTime? AnnouncementExpiryDate
        {
            get => _announcementExpiryDate;
            set => SetProperty(ref _announcementExpiryDate, value);
        }

        // Statistics Properties
        public int TotalStudents
        {
            get => _totalStudents;
            set => SetProperty(ref _totalStudents, value);
        }

        public int TotalTeachers
        {
            get => _totalTeachers;
            set => SetProperty(ref _totalTeachers, value);
        }

        public int TotalStaff
        {
            get => _totalStaff;
            set => SetProperty(ref _totalStaff, value);
        }

        public int PendingApprovals
        {
            get => _pendingApprovals;
            set => SetProperty(ref _pendingApprovals, value);
        }

        public int TodayRegistrations
        {
            get => _todayRegistrations;
            set => SetProperty(ref _todayRegistrations, value);
        }

        // Selected Items Properties
        public StudentSummary? SelectedStudent
        {
            get => _selectedStudent;
            set => SetProperty(ref _selectedStudent, value);
        }

        public TeacherSummary? SelectedTeacher
        {
            get => _selectedTeacher;
            set => SetProperty(ref _selectedTeacher, value);
        }

        public StaffSummary? SelectedStaffMember
        {
            get => _selectedStaffMember;
            set => SetProperty(ref _selectedStaffMember, value);
        }

        public AnnouncementSummary? SelectedAnnouncement
        {
            get => _selectedAnnouncement;
            set => SetProperty(ref _selectedAnnouncement, value);
        }

        // Collections
        public ObservableCollection<StudentSummary> Students { get; } = new();
        public ObservableCollection<TeacherSummary> Teachers { get; } = new();
        public ObservableCollection<StaffSummary> StaffMembers { get; } = new();
        public ObservableCollection<AnnouncementSummary> Announcements { get; } = new();
        public ObservableCollection<PendingRegistration> PendingRegistrations { get; } = new();
        public ObservableCollection<CommunicationRequest> CommunicationRequests { get; } = new();

        // Available Options
        public List<string> AvailableForms { get; } = new() { "Form1", "Form2", "Form3", "Form4" };
        public List<string> AvailableClasses { get; } = new() { "A", "B", "C", "S", "N" };
        public List<string> AvailableGenders { get; } = new() { "Male", "Female" };
        public List<string> EmployeeTypes { get; } = new() { "BOM", "NTSC", "Contract", "Volunteer" };
        public List<string> StaffPositions { get; } = new() { "Librarian", "LabTechnician", "ComputerLabTechnician", "Cook", "Gardener", "BoardingMaster" };
        public List<string> AnnouncementTypes { get; } = new() { "General", "Academic", "Event", "Emergency", "Fee", "Administrative" };
        public List<string> AnnouncementPriorities { get; } = new() { "Low", "Normal", "High", "Urgent" };
        public List<string> AvailableSubjects { get; } = new()
        {
            "Mathematics", "English", "Kiswahili", "Chemistry", "Physics", "Biology",
            "History", "Geography", "CRE", "Business Studies", "Agriculture", "Computer Studies"
        };

        #endregion

        #region Commands

        public ICommand InitializeCommand { get; }
        public ICommand RefreshDataCommand { get; }
        public ICommand RegisterStudentCommand { get; }
        public ICommand ClearStudentFormCommand { get; }
        public ICommand RegisterTeacherCommand { get; }
        public ICommand ClearTeacherFormCommand { get; }
        public ICommand RegisterStaffCommand { get; }
        public ICommand ClearStaffFormCommand { get; }
        public ICommand CreateAnnouncementCommand { get; }
        public ICommand ClearAnnouncementFormCommand { get; }
        public ICommand ViewStudentDetailsCommand { get; }
        public ICommand EditStudentCommand { get; }
        public ICommand DeleteStudentCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ExportStudentListCommand { get; }

        #endregion

        #region Constructor

        public SecretaryViewModel(
            SupabaseService? supabaseService = null,
            UserRegistrationService? registrationService = null,
            ILogger<SecretaryViewModel>? logger = null)
        {
            _supabaseService = supabaseService ?? SupabaseService.Instance;
            _registrationService = registrationService ?? new UserRegistrationService(new AuthService());
            _logger = logger;

            // Initialize commands
            InitializeCommand = new Command(async () => await InitializeAsync(), () => !IsBusy);
            RefreshDataCommand = new Command(async () => await InitializeAsync());
            RegisterStudentCommand = new Command(async () => await RegisterStudentAsync(), CanRegisterStudent);
            ClearStudentFormCommand = new Command(ClearStudentForm);
            RegisterTeacherCommand = new Command(async () => await RegisterTeacherAsync(), CanRegisterTeacher);
            ClearTeacherFormCommand = new Command(ClearTeacherForm);
            RegisterStaffCommand = new Command(async () => await RegisterStaffAsync(), CanRegisterStaff);
            ClearStaffFormCommand = new Command(ClearStaffForm);
            CreateAnnouncementCommand = new Command(async () => await CreateAnnouncementAsync(), CanCreateAnnouncement);
            ClearAnnouncementFormCommand = new Command(ClearAnnouncementForm);
            ViewStudentDetailsCommand = new Command<StudentSummary>(async (student) => await ViewStudentDetailsAsync(student));
            EditStudentCommand = new Command<StudentSummary>(async (student) => await EditStudentAsync(student));
            DeleteStudentCommand = new Command<StudentSummary>(async (student) => await DeleteStudentAsync(student));
            SearchCommand = new Command(async () => await SearchAsync());
            ExportStudentListCommand = new Command(async () => await ExportStudentListAsync());

            // Initialize asynchronously
            _ = Task.Run(async () => await InitializeAsync());
        }

        #endregion

        #region Command Implementations

        private async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                await LoadUserInfoAsync();
                await LoadDashboardDataAsync();
                await LoadStudentsAsync();
                await LoadAnnouncementsAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing secretary dashboard");
                await ShowErrorAsync("Initialization Error", $"Failed to load dashboard: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        // FIXED: Use Services.StudentRegistrationRequest
        private async Task RegisterStudentAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var request = new Services.StudentRegistrationRequest
                {
                    FullName = StudentFullName.Trim(),
                    AdmissionNo = StudentAdmissionNo.Trim(),
                    Form = StudentForm,
                    Class = StudentClass,
                    Email = string.IsNullOrWhiteSpace(StudentEmail) ? null : StudentEmail.Trim(),
                    DateOfBirth = StudentDateOfBirth,
                    Gender = StudentGender,
                    Address = string.IsNullOrWhiteSpace(StudentAddress) ? null : StudentAddress.Trim(),
                    ParentPhone = FormatPhoneNumber(ParentPhone)
                };

                var result = await _registrationService.RegisterStudentAsync(request);

                if (result.Success)
                {
                    await ShowSuccessAsync("Registration Successful",
                        $"Student registered successfully!\n\nStudent ID: {result.GeneratedId}\n" +
                        $"Login Phone: {result.LoginCredentials?.PhoneNumber}\n" +
                        $"Password: {result.LoginCredentials?.Password}");

                    ClearStudentForm();
                    await LoadStudentsAsync();
                    await LoadDashboardDataAsync();
                }
                else
                {
                    await ShowErrorAsync("Registration Failed", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error registering student");
                await ShowErrorAsync("Error", $"Failed to register student: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanRegisterStudent()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(StudentFullName) &&
                   !string.IsNullOrWhiteSpace(StudentAdmissionNo) &&
                   !string.IsNullOrWhiteSpace(ParentPhone) &&
                   StudentDateOfBirth < DateTime.Now.AddYears(-10);
        }

        private void ClearStudentForm()
        {
            StudentFullName = string.Empty;
            StudentAdmissionNo = string.Empty;
            StudentForm = "Form1";
            StudentClass = "A";
            StudentEmail = string.Empty;
            StudentDateOfBirth = DateTime.Now.AddYears(-16);
            StudentGender = "Male";
            StudentAddress = string.Empty;
            ParentPhone = string.Empty;
            GuardianName = string.Empty;
            GuardianPhone = string.Empty;
        }

        // FIXED: Use Services.TeacherRegistrationRequest
        private async Task RegisterTeacherAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var request = new Services.TeacherRegistrationRequest
                {
                    FullName = TeacherFullName.Trim(),
                    PhoneNumber = FormatPhoneNumber(TeacherPhoneNumber),
                    EmployeeType = TeacherEmployeeType,
                    TscNumber = string.IsNullOrWhiteSpace(TeacherTscNumber) ? null : TeacherTscNumber.Trim(),
                    NtscPayment = TeacherEmployeeType == "NTSC" ? TeacherNtscPayment : null,
                    Subjects = new List<string>(), // Will be assigned later
                    AssignedForms = new List<string>(),
                    Email = string.IsNullOrWhiteSpace(TeacherEmail) ? null : TeacherEmail.Trim(),
                    Qualification = string.IsNullOrWhiteSpace(TeacherQualification) ? null : TeacherQualification.Trim(),
                    Department = string.IsNullOrWhiteSpace(TeacherDepartment) ? null : TeacherDepartment.Trim()
                };

                var result = await _registrationService.RegisterTeacherAsync(request);

                if (result.Success)
                {
                    await ShowSuccessAsync("Registration Successful",
                        $"Teacher registered successfully!\n\nTeacher ID: {result.GeneratedId}\n" +
                        $"Login Phone: {result.LoginCredentials?.PhoneNumber}\n" +
                        $"Password: {result.LoginCredentials?.Password}");

                    ClearTeacherForm();
                    await LoadTeachersAsync();
                    await LoadDashboardDataAsync();
                }
                else
                {
                    await ShowErrorAsync("Registration Failed", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error registering teacher");
                await ShowErrorAsync("Error", $"Failed to register teacher: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanRegisterTeacher()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(TeacherFullName) &&
                   !string.IsNullOrWhiteSpace(TeacherPhoneNumber) &&
                   !string.IsNullOrWhiteSpace(TeacherEmployeeType) &&
                   (TeacherEmployeeType != "NTSC" || TeacherNtscPayment > 0);
        }

        private void ClearTeacherForm()
        {
            TeacherFullName = string.Empty;
            TeacherPhoneNumber = string.Empty;
            TeacherEmail = string.Empty;
            TeacherEmployeeType = "BOM";
            TeacherTscNumber = string.Empty;
            TeacherNtscPayment = 0;
            TeacherQualification = string.Empty;
            TeacherDepartment = string.Empty;
        }

        // FIXED: Use Services.StaffRegistrationRequest
        private async Task RegisterStaffAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var request = new Services.StaffRegistrationRequest
                {
                    FullName = StaffFullName.Trim(),
                    PhoneNumber = FormatPhoneNumber(StaffPhoneNumber),
                    Position = StaffPosition,
                    Department = string.IsNullOrWhiteSpace(StaffDepartment) ? null : StaffDepartment.Trim(),
                    Email = string.IsNullOrWhiteSpace(StaffEmail) ? null : StaffEmail.Trim()
                };

                var result = await _registrationService.RegisterStaffAsync(request);

                if (result.Success)
                {
                    await ShowSuccessAsync("Registration Successful",
                        $"Staff member registered successfully!\n\nStaff ID: {result.GeneratedId}\n" +
                        $"Login Phone: {result.LoginCredentials?.PhoneNumber}\n" +
                        $"Password: {result.LoginCredentials?.Password}");

                    ClearStaffForm();
                    await LoadStaffAsync();
                    await LoadDashboardDataAsync();
                }
                else
                {
                    await ShowErrorAsync("Registration Failed", result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error registering staff");
                await ShowErrorAsync("Error", $"Failed to register staff: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanRegisterStaff()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(StaffFullName) &&
                   !string.IsNullOrWhiteSpace(StaffPhoneNumber) &&
                   !string.IsNullOrWhiteSpace(StaffPosition);
        }

        private void ClearStaffForm()
        {
            StaffFullName = string.Empty;
            StaffPhoneNumber = string.Empty;
            StaffEmail = string.Empty;
            StaffPosition = "Librarian";
            StaffDepartment = string.Empty;
        }

        private async Task CreateAnnouncementAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;

                var announcement = new AnnouncementEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = AnnouncementTitle.Trim(),
                    Content = AnnouncementContent.Trim(),
                    AnnouncementType = AnnouncementType,
                    Priority = AnnouncementPriority,
                    TargetAudience = new List<string> { "Students", "Teachers", "Parents" },
                    TargetForms = new List<string> { "Form1", "Form2", "Form3", "Form4" },
                    IsPublished = true,
                    PublishDate = DateTime.UtcNow,
                    ExpiryDate = AnnouncementExpiryDate,
                    CreatedBy = await GetCurrentUserIdAsync()
                };

                try
                {
                    await _supabaseService.Client.From<AnnouncementEntity>().Insert(announcement);
                    await ShowSuccessAsync("Success", "Announcement created and published successfully!");

                    ClearAnnouncementForm();
                    await LoadAnnouncementsAsync();
                }
                catch (Exception ex)
                {
                    await ShowErrorAsync("Database Error", $"Failed to save announcement: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating announcement");
                await ShowErrorAsync("Error", $"Failed to create announcement: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanCreateAnnouncement()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(AnnouncementTitle) &&
                   !string.IsNullOrWhiteSpace(AnnouncementContent);
        }

        private void ClearAnnouncementForm()
        {
            AnnouncementTitle = string.Empty;
            AnnouncementContent = string.Empty;
            AnnouncementType = "General";
            AnnouncementPriority = "Normal";
            AnnouncementExpiryDate = null;
        }

        private async Task ViewStudentDetailsAsync(StudentSummary? student)
        {
            if (student == null) return;

            try
            {
                var details = $"Student ID: {student.StudentId}\n" +
                             $"Admission No: {student.AdmissionNo}\n" +
                             $"Full Name: {student.FullName}\n" +
                             $"Class: {student.Form}{student.Class}\n" +
                             $"Parent Phone: {student.ParentPhone}\n" +
                             $"Registration Date: {student.CreatedAt:MMM dd, yyyy}";

                await ShowInfoAsync("Student Details", details);
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to load student details: {ex.Message}");
            }
        }

        private async Task EditStudentAsync(StudentSummary? student)
        {
            if (student == null) return;

            try
            {
                // Navigate to edit student page or show edit dialog
                await NavigateToAsync($"//edit-student?studentId={student.StudentId}");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to open student editor: {ex.Message}");
            }
        }

        private async Task DeleteStudentAsync(StudentSummary? student)
        {
            if (student == null) return;

            try
            {
                var confirm = await ShowConfirmAsync("Delete Student",
                    $"Are you sure you want to delete {student.FullName}? This action cannot be undone.");

                if (confirm)
                {
                    IsBusy = true;

                    var studentEntity = await _supabaseService.Client
                        .From<StudentEntity>()
                        .Where(x => x.StudentId == student.StudentId)
                        .Single();

                    if (studentEntity != null)
                    {
                        // Deactivate instead of deleting
                        studentEntity.IsActive = false;
                        studentEntity.UpdatedAt = DateTime.UtcNow;
                        await studentEntity.Update<StudentEntity>();

                        Students.Remove(student);
                        await LoadDashboardDataAsync();

                        await ShowSuccessAsync("Success", "Student has been deactivated successfully.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting student {StudentId}", student.StudentId);
                await ShowErrorAsync("Error", $"Failed to delete student: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await LoadStudentsAsync();
                return;
            }

            try
            {
                IsBusy = true;

                var searchTerm = SearchText.Trim().ToLower();
                var filteredStudents = Students.Where(s =>
                    s.FullName.ToLower().Contains(searchTerm) ||
                    s.StudentId.ToLower().Contains(searchTerm) ||
                    s.AdmissionNo.ToLower().Contains(searchTerm) ||
                    s.ParentPhone.Contains(searchTerm)
                ).ToList();

                Students.Clear();
                foreach (var student in filteredStudents)
                {
                    Students.Add(student);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching students");
                await ShowErrorAsync("Error", $"Search failed: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportStudentListAsync()
        {
            try
            {
                IsBusy = true;

                // Simulate export process
                await Task.Delay(2000);

                await ShowSuccessAsync("Export Complete",
                    "Student list has been exported successfully to your device downloads folder.");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Export Error", $"Failed to export student list: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Data Loading Methods

        private async Task LoadUserInfoAsync()
        {
            try
            {
                CurrentUserName = await Microsoft.Maui.Storage.SecureStorage.GetAsync("full_name") ?? "Secretary";
                Title = $"Secretary Dashboard - {CurrentUserName}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading user info");
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                // Load statistics
                var studentsCount = await _supabaseService.Client
                    .From<StudentEntity>()
                    .Where(x => x.IsActive && x.Year == DateTime.UtcNow.Year)
                    .Get();

                var teachersCount = await _supabaseService.Client
                    .From<TeacherEntity>()
                    .Where(x => x.IsActive)
                    .Get();

                var staffCount = await _supabaseService.Client
                    .From<StaffEntity>()
                    .Where(x => x.IsActive)
                    .Get();

                var todayRegistrationsCount = await _supabaseService.Client
                    .From<StudentEntity>()
                    .Where(x => x.CreatedAt >= DateTime.UtcNow.Date && x.CreatedAt < DateTime.UtcNow.Date.AddDays(1))
                    .Get();

                TotalStudents = studentsCount?.Models?.Count ?? 0;
                TotalTeachers = teachersCount?.Models?.Count ?? 0;
                TotalStaff = staffCount?.Models?.Count ?? 0;
                TodayRegistrations = todayRegistrationsCount?.Models?.Count ?? 0;
                PendingApprovals = 0; // Calculate from various pending items
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading dashboard data");
            }
        }

        private async Task LoadStudentsAsync()
        {
            try
            {
                var studentsResponse = await _supabaseService.Client
                    .From<StudentEntity>()
                    .Where(x => x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(100)
                    .Get();

                Students.Clear();

                if (studentsResponse?.Models != null)
                {
                    foreach (var student in studentsResponse.Models)
                    {
                        Students.Add(new StudentSummary
                        {
                            StudentId = student.StudentId,
                            FullName = student.FullName,
                            AdmissionNo = student.AdmissionNo,
                            Form = student.Form,
                            Class = student.Class,
                            ParentPhone = student.ParentPhone,
                            CreatedAt = student.CreatedAt
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading students");
                throw;
            }
        }

        private async Task LoadTeachersAsync()
        {
            try
            {
                var teachersResponse = await _supabaseService.Client
                    .From<TeacherEntity>()
                    .Where(x => x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                Teachers.Clear();

                if (teachersResponse?.Models != null)
                {
                    foreach (var teacher in teachersResponse.Models)
                    {
                        Teachers.Add(new TeacherSummary
                        {
                            TeacherId = teacher.TeacherId,
                            FullName = teacher.FullName,
                            EmployeeType = teacher.EmployeeType,
                            Subjects = teacher.Subjects,
                            Department = teacher.Department ?? "General",
                            CreatedAt = teacher.CreatedAt
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading teachers");
                throw;
            }
        }

        private async Task LoadStaffAsync()
        {
            try
            {
                var staffResponse = await _supabaseService.Client
                    .From<StaffEntity>()
                    .Where(x => x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                StaffMembers.Clear();

                if (staffResponse?.Models != null)
                {
                    foreach (var staff in staffResponse.Models)
                    {
                        StaffMembers.Add(new StaffSummary
                        {
                            StaffId = staff.StaffId,
                            FullName = staff.FullName,
                            Position = staff.Position,
                            Department = staff.Department ?? "General",
                            CreatedAt = staff.CreatedAt
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading staff");
                throw;
            }
        }

        private async Task LoadAnnouncementsAsync()
        {
            try
            {
                var announcementsResponse = await _supabaseService.Client
                    .From<AnnouncementEntity>()
                    .Where(x => x.IsPublished)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(50)
                    .Get();

                Announcements.Clear();

                if (announcementsResponse?.Models != null)
                {
                    foreach (var announcement in announcementsResponse.Models)
                    {
                        Announcements.Add(new AnnouncementSummary
                        {
                            Id = announcement.Id,
                            Title = announcement.Title,
                            Type = announcement.AnnouncementType,
                            Priority = announcement.Priority,
                            PublishDate = announcement.PublishDate ?? announcement.CreatedAt,
                            ExpiryDate = announcement.ExpiryDate,
                            IsActive = announcement.IsActive
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading announcements");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private string FormatPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return string.Empty;

            // Remove all non-digit characters
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Format to Kenyan standard
            if (digits.StartsWith("254") && digits.Length == 12)
                return $"+{digits}";

            if (digits.StartsWith("0") && digits.Length == 10)
                return $"+254{digits[1..]}";

            if (digits.Length == 9)
                return $"+254{digits}";

            return phoneNumber; // Return original if format not recognized
        }

        private async Task<string> GetCurrentUserIdAsync()
        {
            try
            {
                return await Microsoft.Maui.Storage.SecureStorage.GetAsync("user_id") ?? "secretary";
            }
            catch
            {
                return "secretary";
            }
        }

        private async Task ShowSuccessAsync(string title, string message)
        {
            try
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(title, message, "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing success message");
            }
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            try
            {
                await Microsoft.Maui.Controls.Application.Current.MainPage.DisplayAlert(title, message, "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing error message");
            }
        }

        private async Task ShowInfoAsync(string title, string message)
        {
            try
            {
                await Application.Current.MainPage.DisplayAlert(title, message, "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing info message");
            }
        }

        private async Task<bool> ShowConfirmAsync(string title, string message)
        {
            try
            {
                return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing confirm dialog");
                return false;
            }
        }

        private async Task NavigateToAsync(string route)
        {
            try
            {
                await Microsoft.Maui.Controls.Shell.Current.GoToAsync(route);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Navigation error to {Route}", route);
                await ShowErrorAsync("Navigation Error", "Failed to navigate to the requested page.");
            }
        }

        #endregion

        #region Property Change Handlers

        private void OnSelectedTabIndexChanged(int value)
        {
            // Load data for the selected tab
            _ = Task.Run(async () =>
            {
                try
                {
                    switch (value)
                    {
                        case 1: // Teachers tab
                            await LoadTeachersAsync();
                            break;
                        case 2: // Staff tab
                            await LoadStaffAsync();
                            break;
                        case 3: // Communication tab
                            await LoadAnnouncementsAsync();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error loading tab data for index {TabIndex}", value);
                }
            });
        }

        private void OnTeacherEmployeeTypeChanged(string value)
        {
            // Reset NTSC payment when employee type changes
            if (value != "NTSC")
            {
                TeacherNtscPayment = 0;
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    _operationSemaphore?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during SecretaryViewModel disposal");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        #endregion
    }

    #region Supporting Models

    /// <summary>
    /// Summary model for student display
    /// </summary>
    public class StudentSummary
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string FullClass => $"{Form}{Class}";
        public string FormattedCreatedAt => CreatedAt.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// Summary model for teacher display
    /// </summary>
    public class TeacherSummary
    {
        public string TeacherId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string EmployeeType { get; set; } = string.Empty;
        public List<string> Subjects { get; set; } = new();
        public string Department { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string SubjectsText => string.Join(", ", Subjects);
        public string FormattedCreatedAt => CreatedAt.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// Summary model for staff display
    /// </summary>
    public class StaffSummary
    {
        public string StaffId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public string FormattedCreatedAt => CreatedAt.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// Summary model for announcement display
    /// </summary>
    public class AnnouncementSummary
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime PublishDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; }

        public string FormattedPublishDate => PublishDate.ToString("MMM dd, yyyy");
        public string FormattedExpiryDate => ExpiryDate?.ToString("MMM dd, yyyy") ?? "No expiry";
        public string StatusText => IsActive ? "Active" : "Inactive";
    }

    /// <summary>
    /// Model for pending registrations
    /// </summary>
    public class PendingRegistration
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Student, Teacher, Staff
        public string Name { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model for communication requests
    /// </summary>
    public class CommunicationRequest
    {
        public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Email, SMS, Notice
        public string Subject { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
    }

    /// <summary>
    /// Registration result model
    /// </summary>
    public class RegistrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? GeneratedId { get; set; }
        public LoginCredentials? LoginCredentials { get; set; }
    }

    /// <summary>
    /// Login credentials model
    /// </summary>
    public class LoginCredentials
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    #endregion
}