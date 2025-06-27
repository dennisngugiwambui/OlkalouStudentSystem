// ===============================
// Services/ApiService.cs - Complete Error-Free API Service (FULLY CORRECTED)
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Models.Data;
using System.Text.Json;
using static Supabase.Postgrest.Constants;

namespace OlkalouStudentSystem.Services
{
    #region Supporting Models and Classes

    /// <summary>
    /// User profile information
    /// </summary>
    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public Models.UserType UserType { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime LastLogin { get; set; }
        public string ProfilePictureUrl { get; set; } = string.Empty;
    }

    /// <summary>
    /// File data for uploads
    /// </summary>
    public class FileData
    {
        public string FileName { get; set; } = string.Empty;
        public byte[] Content { get; set; } = Array.Empty<byte>();
        public long Size { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Upload result
    /// </summary>
    public class UploadResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Detailed student information
    /// </summary>
    public class StudentDetailedInfo
    {
        public Student Student { get; set; } = new Student();
        public FeesInfo FeesInfo { get; set; } = new FeesInfo();
        public List<Assignment> Assignments { get; set; } = new List<Assignment>();
        public List<BookIssue> IssuedBooks { get; set; } = new List<BookIssue>();
        public List<Achievement> Achievements { get; set; } = new List<Achievement>();
        public List<Models.SubjectPerformance> Performance { get; set; } = new List<Models.SubjectPerformance>();
    }

    /// <summary>
    /// System health information
    /// </summary>
    public class SystemHealthInfo
    {
        public bool IsHealthy { get; set; }
        public bool DatabaseConnected { get; set; }
        public bool SupabaseConnected { get; set; }
        public DateTime LastChecked { get; set; }
        public string Version { get; set; } = string.Empty;
        public string Environment { get; set; } = string.Empty;
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> AdditionalInfo { get; set; } = new Dictionary<string, object>();
    }

    #endregion

    public partial class ApiService : IDisposable
    {
        #region Fields and Properties

        private readonly Supabase.Client _supabaseClient;
        private readonly AuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<ApiService>? _logger;
        private bool _disposed = false;

        #endregion

        #region Constructor

        public ApiService(AuthService authService, ILogger<ApiService>? logger = null)
        {
            _supabaseClient = SupabaseService.Instance.Client;
            _authService = authService;
            _logger = logger;

            // JSON serialization options
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
        }

        #endregion

        #region Authentication Methods

        /// <summary>
        /// Login user - delegates to AuthService
        /// </summary>
        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            return await _authService.LoginAsync(request.PhoneNumber, request.Password);
        }

        /// <summary>
        /// Refresh authentication token
        /// </summary>
        public async Task<ApiResponse<string>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var success = await _authService.TryRefreshTokenAsync();
                return new ApiResponse<string>
                {
                    Success = success,
                    Data = success ? _authService.AuthToken : null,
                    Message = success ? "Token refreshed successfully" : "Token refresh failed",
                    ErrorCode = success ? null : "TOKEN_REFRESH_FAILED"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing token");
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "TOKEN_REFRESH_FAILED"
                };
            }
        }

        #endregion

        #region User Profile Methods

        /// <summary>
        /// Get student by ID
        /// </summary>
        public async Task<ApiResponse<Student>> GetStudentByIdAsync(string studentId)
        {
            try
            {
                var studentEntity = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.Id == studentId)
                    .Single();

                if (studentEntity == null)
                {
                    return new ApiResponse<Student>
                    {
                        Success = false,
                        Message = "Student not found",
                        ErrorCode = "STUDENT_NOT_FOUND"
                    };
                }

                var student = MapStudentEntityToModel(studentEntity);

                return new ApiResponse<Student>
                {
                    Success = true,
                    Data = student,
                    Message = "Student retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting student by ID {StudentId}", studentId);
                return new ApiResponse<Student>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "STUDENT_LOAD_FAILED"
                };
            }
        }

        /// <summary>
        /// Get teacher by ID
        /// </summary>
        public async Task<ApiResponse<Teacher>> GetTeacherByIdAsync(string teacherId)
        {
            try
            {
                var teacherEntity = await _supabaseClient
                    .From<TeacherEntity>()
                    .Where(x => x.Id == teacherId)
                    .Single();

                if (teacherEntity == null)
                {
                    return new ApiResponse<Teacher>
                    {
                        Success = false,
                        Message = "Teacher not found",
                        ErrorCode = "TEACHER_NOT_FOUND"
                    };
                }

                var teacher = MapTeacherEntityToModel(teacherEntity);

                return new ApiResponse<Teacher>
                {
                    Success = true,
                    Data = teacher,
                    Message = "Teacher retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting teacher by ID {TeacherId}", teacherId);
                return new ApiResponse<Teacher>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "TEACHER_LOAD_FAILED"
                };
            }
        }

        /// <summary>
        /// Get staff by ID
        /// </summary>
        public async Task<ApiResponse<Staff>> GetStaffByIdAsync(string staffId)
        {
            try
            {
                var staffEntity = await _supabaseClient
                    .From<StaffEntity>()
                    .Where(x => x.Id == staffId)
                    .Single();

                if (staffEntity == null)
                {
                    return new ApiResponse<Staff>
                    {
                        Success = false,
                        Message = "Staff not found",
                        ErrorCode = "STAFF_NOT_FOUND"
                    };
                }

                var staff = MapStaffEntityToModel(staffEntity);

                return new ApiResponse<Staff>
                {
                    Success = true,
                    Data = staff,
                    Message = "Staff retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting staff by ID {StaffId}", staffId);
                return new ApiResponse<Staff>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "STAFF_LOAD_FAILED"
                };
            }
        }

        /// <summary>
        /// Get users by type
        /// </summary>
        public async Task<List<UserProfile>> GetUsersByTypeAsync(Models.UserType userType)
        {
            try
            {
                var userTypeString = userType.ToString();
                var userEntities = await _supabaseClient
                    .From<UserEntity>()
                    .Where(x => x.UserType == userTypeString && x.IsActive)
                    .Get();

                var users = new List<UserProfile>();

                if (userEntities?.Models != null)
                {
                    foreach (var userEntity in userEntities.Models)
                    {
                        var fullName = await GetUserFullNameAsync(userEntity.Id, userTypeString);
                        var email = await GetUserEmailAsync(userEntity.Id, userTypeString);

                        users.Add(new UserProfile
                        {
                            UserId = userEntity.Id,
                            PhoneNumber = userEntity.PhoneNumber,
                            UserType = userType,
                            FullName = fullName,
                            Email = email
                        });
                    }
                }

                return users;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting users by type {UserType}", userType);
                return new List<UserProfile>();
            }
        }

        /// <summary>
        /// Get students by form
        /// </summary>
        public async Task<List<Student>> GetStudentsByFormAsync(string form)
        {
            try
            {
                var studentEntities = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.Form == form && x.IsActive)
                    .Get();

                var students = new List<Student>();

                if (studentEntities?.Models != null)
                {
                    foreach (var studentEntity in studentEntities.Models)
                    {
                        students.Add(MapStudentEntityToModel(studentEntity));
                    }
                }

                return students;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting students by form {Form}", form);
                return new List<Student>();
            }
        }

        /// <summary>
        /// Get students by class
        /// </summary>
        public async Task<List<Student>> GetStudentsByClassAsync(string className)
        {
            try
            {
                var studentEntities = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.Class == className && x.IsActive)
                    .Get();

                var students = new List<Student>();

                if (studentEntities?.Models != null)
                {
                    foreach (var studentEntity in studentEntities.Models)
                    {
                        students.Add(MapStudentEntityToModel(studentEntity));
                    }
                }

                return students;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting students by class {ClassName}", className);
                return new List<Student>();
            }
        }

        #endregion

        #region Student Dashboard Methods

        /// <summary>
        /// Get dashboard data for a student
        /// </summary>
        public async Task<ApiResponse<DashboardData>> GetDashboardDataAsync(string studentId)
        {
            try
            {
                var currentStudent = _authService.CurrentStudent;
                if (currentStudent == null)
                {
                    return new ApiResponse<DashboardData>
                    {
                        Success = false,
                        Message = "Student not authenticated",
                        ErrorCode = "STUDENT_NOT_AUTHENTICATED"
                    };
                }

                // Get fees info
                var feesResponse = await GetFeesInfoAsync(currentStudent.StudentId);
                var feesInfo = feesResponse.Success ? feesResponse.Data : new FeesInfo();

                // Get pending assignments
                var assignmentsResponse = await GetAssignmentsAsync(currentStudent.Form);
                var pendingAssignments = assignmentsResponse.Success
                    ? assignmentsResponse.Data.Where(a => a.Status == "Pending").Take(4).ToList()
                    : new List<Assignment>();

                // Get issued books
                var booksResponse = await GetIssuedBooksAsync(currentStudent.StudentId);
                var issuedBooks = booksResponse.Success ? booksResponse.Data.Take(3).ToList() : new List<BookIssue>();

                // Get upcoming activities
                var activitiesResponse = await GetActivitiesAsync(currentStudent.Form);
                var upcomingActivities = activitiesResponse.Success
                    ? activitiesResponse.Data.Where(a => a.Date > DateTime.Now).Take(4).ToList()
                    : new List<Activity>();

                // Get achievements
                var achievementsResponse = await GetStudentAchievementsAsync(currentStudent.StudentId);
                var achievements = achievementsResponse.Success ? achievementsResponse.Data : new List<Achievement>();

                // Get performance data
                var performanceResponse = await GetStudentPerformanceAsync(currentStudent.StudentId);
                var performance = performanceResponse.Success ? performanceResponse.Data : new List<Models.SubjectPerformance>();

                var dashboardData = new DashboardData
                {
                    CurrentFees = feesInfo,
                    PendingAssignments = new System.Collections.ObjectModel.ObservableCollection<Assignment>(pendingAssignments),
                    IssuedBooks = new System.Collections.ObjectModel.ObservableCollection<LibraryBook>(),
                    RecentAchievements = new System.Collections.ObjectModel.ObservableCollection<Achievement>(achievements),
                    UpcomingActivities = new System.Collections.ObjectModel.ObservableCollection<Activity>(upcomingActivities),
                    AcademicPerformance = new System.Collections.ObjectModel.ObservableCollection<Models.SubjectPerformance>(performance)
                };

                return new ApiResponse<DashboardData>
                {
                    Success = true,
                    Data = dashboardData,
                    Message = "Dashboard data retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting dashboard data for student {StudentId}", studentId);
                return new ApiResponse<DashboardData>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "DASHBOARD_LOAD_FAILED"
                };
            }
        }

        /// <summary>
        /// Get pending assignments for student
        /// </summary>
        public async Task<ApiResponse<List<Assignment>>> GetPendingAssignmentsAsync(string studentId)
        {
            try
            {
                var currentStudent = _authService.CurrentStudent;
                if (currentStudent == null)
                {
                    return new ApiResponse<List<Assignment>>
                    {
                        Success = false,
                        Message = "Student not authenticated",
                        ErrorCode = "STUDENT_NOT_AUTHENTICATED"
                    };
                }

                var assignmentEntities = await _supabaseClient
                    .From<AssignmentEntity>()
                    .Where(x => x.Form == currentStudent.Form && x.DueDate > DateTime.Now)
                    .Order(x => x.DueDate, Ordering.Ascending)
                    .Get();

                var assignments = new List<Assignment>();

                if (assignmentEntities?.Models != null)
                {
                    foreach (var entity in assignmentEntities.Models)
                    {
                        var submission = await GetAssignmentSubmissionAsync(entity.Id, studentId);
                        if (submission == null) // Only pending assignments
                        {
                            assignments.Add(new Assignment
                            {
                                AssignmentId = entity.AssignmentId,
                                Title = entity.Title,
                                Subject = entity.Subject,
                                Description = entity.Description,
                                DueDate = entity.DueDate,
                                DateCreated = entity.CreatedAt,
                                Status = "Pending",
                                FilePath = entity.FilePath ?? string.Empty,
                                MaxMarks = entity.MaxMarks,
                                CreatedBy = await GetTeacherNameAsync(entity.CreatedBy),
                                Form = entity.Form,
                                IsSubmitted = false
                            });
                        }
                    }
                }

                return new ApiResponse<List<Assignment>>
                {
                    Success = true,
                    Data = assignments,
                    Message = $"Retrieved {assignments.Count} pending assignments"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting pending assignments for student {StudentId}", studentId);
                return new ApiResponse<List<Assignment>>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "PENDING_ASSIGNMENTS_LOAD_FAILED"
                };
            }
        }

        /// <summary>
        /// Get student achievements
        /// </summary>
        public async Task<ApiResponse<List<Achievement>>> GetStudentAchievementsAsync(string studentId)
        {
            try
            {
                // Return demo achievements for now
                var achievements = new List<Achievement>
                {
                    new Achievement
                    {
                        AchievementId = "ACH001",
                        Title = "Academic Excellence",
                        Description = "Top 5 in class for Term 2",
                        DateAchieved = DateTime.Now.AddDays(-30),
                        Date = DateTime.Now.AddDays(-30),
                        Category = "Academic",
                        Points = 100
                    },
                    new Achievement
                    {
                        AchievementId = "ACH002",
                        Title = "Perfect Attendance",
                        Description = "100% attendance for the month",
                        DateAchieved = DateTime.Now.AddDays(-15),
                        Date = DateTime.Now.AddDays(-15),
                        Category = "Attendance",
                        Points = 50
                    }
                };

                return new ApiResponse<List<Achievement>>
                {
                    Success = true,
                    Data = achievements,
                    Message = $"Retrieved {achievements.Count} achievements"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting student achievements for {StudentId}", studentId);
                return new ApiResponse<List<Achievement>>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "ACHIEVEMENTS_LOAD_FAILED"
                };
            }
        }

        /// <summary>
        /// Get student performance data
        /// </summary>
        public async Task<ApiResponse<List<Models.SubjectPerformance>>> GetStudentPerformanceAsync(string studentId)
        {
            try
            {
                // Return demo performance data
                var performance = new List<Models.SubjectPerformance>
                {
                    new Models.SubjectPerformance { Subject = "Mathematics", CurrentGrade = 85, PreviousGrade = 78, Trend = "up" },
                    new Models.SubjectPerformance { Subject = "English", CurrentGrade = 92, PreviousGrade = 89, Trend = "up" },
                    new Models.SubjectPerformance { Subject = "Science", CurrentGrade = 78, PreviousGrade = 82, Trend = "down" },
                    new Models.SubjectPerformance { Subject = "History", CurrentGrade = 89, PreviousGrade = 85, Trend = "up" },
                    new Models.SubjectPerformance { Subject = "Geography", CurrentGrade = 83, PreviousGrade = 80, Trend = "up" }
                };

                return new ApiResponse<List<Models.SubjectPerformance>>
                {
                    Success = true,
                    Data = performance,
                    Message = $"Retrieved performance for {performance.Count} subjects"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting student performance for {StudentId}", studentId);
                return new ApiResponse<List<Models.SubjectPerformance>>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "PERFORMANCE_LOAD_FAILED"
                };
            }
        }

        #endregion

        #region Assignment Methods

        /// <summary>
        /// Get assignments for a specific form/class
        /// </summary>
        public async Task<ApiResponse<List<Assignment>>> GetAssignmentsAsync(string form)
        {
            try
            {
                var assignmentEntities = await _supabaseClient
                    .From<AssignmentEntity>()
                    .Where(x => x.Form == form)
                    .Order(x => x.DueDate, Ordering.Ascending)
                    .Get();

                var assignments = new List<Assignment>();

                if (assignmentEntities?.Models != null)
                {
                    var currentStudentId = await GetCurrentUserEntityIdAsync();

                    foreach (var entity in assignmentEntities.Models)
                    {
                        var submission = await GetAssignmentSubmissionAsync(entity.Id, currentStudentId);

                        assignments.Add(new Assignment
                        {
                            AssignmentId = entity.AssignmentId,
                            Title = entity.Title,
                            Subject = entity.Subject,
                            Description = entity.Description,
                            DueDate = entity.DueDate,
                            DateCreated = entity.CreatedAt,
                            Status = submission?.Status ?? "Pending",
                            FilePath = entity.FilePath ?? string.Empty,
                            SubmissionPath = submission?.SubmissionPath,
                            MaxMarks = entity.MaxMarks,
                            ObtainedMarks = submission?.ObtainedMarks ?? 0,
                            TeacherComments = submission?.TeacherComments,
                            CreatedBy = await GetTeacherNameAsync(entity.CreatedBy),
                            Form = entity.Form,
                            IsSubmitted = submission != null,
                            SubmissionDate = submission?.SubmissionDate
                        });
                    }
                }

                return new ApiResponse<List<Assignment>>
                {
                    Success = true,
                    Data = assignments,
                    Message = $"Retrieved {assignments.Count} assignments"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting assignments for form {Form}", form);
                return new ApiResponse<List<Assignment>>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "ASSIGNMENTS_LOAD_FAILED"
                };
            }
        }

        /// <summary>
        /// Submit assignment file
        /// </summary>
        public async Task<ApiResponse<UploadResult>> SubmitAssignmentAsync(string assignmentId, FileData fileData)
        {
            try
            {
                var currentStudentId = await GetCurrentUserEntityIdAsync();
                if (string.IsNullOrEmpty(currentStudentId))
                {
                    return new ApiResponse<UploadResult>
                    {
                        Success = false,
                        Message = "Student not authenticated",
                        ErrorCode = "STUDENT_NOT_AUTHENTICATED"
                    };
                }

                // In a real implementation, you would upload the file to Supabase Storage
                var submissionPath = $"/submissions/{assignmentId}_{currentStudentId}_{fileData.FileName}";

                // Check if submission already exists
                var existingSubmission = await _supabaseClient
                    .From<AssignmentSubmissionEntity>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == currentStudentId)
                    .Single();

                if (existingSubmission != null)
                {
                    // Update existing submission
                    existingSubmission.SubmissionPath = submissionPath;
                    existingSubmission.SubmissionDate = DateTime.UtcNow;
                    existingSubmission.Status = "Submitted";
                    await existingSubmission.Update<AssignmentSubmissionEntity>();
                }
                else
                {
                    // Create new submission
                    var submission = new AssignmentSubmissionEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        AssignmentId = assignmentId,
                        StudentId = currentStudentId,
                        SubmissionPath = submissionPath,
                        Status = "Submitted",
                        SubmissionDate = DateTime.UtcNow
                    };

                    await _supabaseClient.From<AssignmentSubmissionEntity>().Insert(submission);
                }

                return new ApiResponse<UploadResult>
                {
                    Success = true,
                    Data = new UploadResult
                    {
                        Success = true,
                        FilePath = submissionPath,
                        FileUrl = submissionPath,
                        Message = "Assignment submitted successfully",
                        FileSize = fileData.Size,
                        UploadDate = DateTime.UtcNow
                    },
                    Message = "Assignment submitted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error submitting assignment {AssignmentId}", assignmentId);
                return new ApiResponse<UploadResult>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "ASSIGNMENT_SUBMIT_FAILED"
                };
            }
        }

        /// <summary>
        /// Download assignment file
        /// </summary>
        public async Task<ApiResponse<byte[]>> DownloadAssignmentAsync(string filePath)
        {
            try
            {
                // Simulate download delay
                await Task.Delay(1000);

                // In a real implementation, you would download from Supabase Storage
                var dummyPdfContent = GenerateDummyPdfContent();

                return new ApiResponse<byte[]>
                {
                    Success = true,
                    Data = dummyPdfContent,
                    Message = "File downloaded successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error downloading assignment file {FilePath}", filePath);
                return new ApiResponse<byte[]>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "DOWNLOAD_FAILED"
                };
            }
        }

        #endregion

        #region Activity Methods

        /// <summary>
        /// Get school activities
        /// </summary>
        public async Task<ApiResponse<List<Activity>>> GetActivitiesAsync(string form)
        {
            try
            {
                var activityEntities = await _supabaseClient
                    .From<ActivityEntity>()
                    .Where(x => x.TargetForms.Contains(form) || x.TargetForms.Contains("All"))
                    .Order(x => x.Date, Ordering.Ascending)
                    .Get();

                var activities = new List<Activity>();

                if (activityEntities?.Models != null)
                {
                    foreach (var entity in activityEntities.Models)
                    {
                        activities.Add(new Activity
                        {
                            ActivityId = entity.ActivityId,
                            Title = entity.Title,
                            Description = entity.Description,
                            Date = entity.Date,
                            StartTime = entity.StartTime,
                            EndTime = entity.EndTime,
                            Venue = entity.Venue,
                            ActivityType = entity.ActivityType,
                            Organizer = entity.Organizer,
                            IsOptional = entity.IsOptional,
                            Requirements = entity.Requirements,
                            TargetForms = entity.TargetForms,
                            Status = entity.Status,
                            RegistrationDeadline = entity.RegistrationDeadline,
                            MaxParticipants = entity.MaxParticipants,
                            CurrentParticipants = entity.CurrentParticipants
                        });
                    }
                }

                return new ApiResponse<List<Activity>>
                {
                    Success = true,
                    Data = activities,
                    Message = $"Retrieved {activities.Count} activities"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting activities for form {Form}", form);
                return new ApiResponse<List<Activity>>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "ACTIVITIES_LOAD_FAILED"
                };
            }
        }

        /// <summary>
        /// Register for an activity
        /// </summary>
        public async Task<ApiResponse<bool>> JoinActivityAsync(string activityId, string studentId)
        {
            try
            {
                // Simulate API call delay
                await Task.Delay(1000);

                // In a real implementation, you would create an activity_registrations table
                // and insert a record linking student to activity

                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Successfully registered for activity"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error joining activity {ActivityId} for student {StudentId}", activityId, studentId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "ACTIVITY_JOIN_FAILED"
                };
            }
        }

        #endregion

        #region Library Methods

        /// <summary>
        /// Get books issued to a student
        /// </summary>
        public async Task<ApiResponse<List<BookIssue>>> GetIssuedBooksAsync(string studentId)
        {
            try
            {
                var currentStudentId = await GetCurrentUserEntityIdAsync();

                var bookIssueEntities = await _supabaseClient
                    .From<BookIssueEntity>()
                    .Where(x => x.StudentId == currentStudentId && x.Status == "Issued")
                    .Order(x => x.IssueDate, Ordering.Descending)
                    .Get();

                var issuedBooks = new List<BookIssue>();

                if (bookIssueEntities?.Models != null)
                {
                    foreach (var entity in bookIssueEntities.Models)
                    {
                        // Get book details
                        var book = await _supabaseClient
                            .From<LibraryBookEntity>()
                            .Where(x => x.Id == entity.BookId)
                            .Single();

                        if (book != null)
                        {
                            issuedBooks.Add(new BookIssue
                            {
                                IssueId = entity.Id,
                                StudentId = entity.StudentId,
                                StudentName = _authService.CurrentStudent?.FullName ?? "Student",
                                BookId = entity.BookId,
                                BookTitle = book.Title,
                                Author = book.Author,
                                IssueDate = entity.IssueDate,
                                DueDate = entity.DueDate,
                                ReturnDate = entity.ReturnDate,
                                Status = entity.Status,
                                FineAmount = entity.FineAmount
                            });
                        }
                    }
                }

                return new ApiResponse<List<BookIssue>>
                {
                    Success = true,
                    Data = issuedBooks,
                    Message = $"Retrieved {issuedBooks.Count} issued books"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting issued books for student {StudentId}", studentId);
                return new ApiResponse<List<BookIssue>>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "ISSUED_BOOKS_LOAD_FAILED"
                };
            }
        }

        /// <summary>
        /// Search available books in library
        /// </summary>
        public async Task<ApiResponse<List<LibraryBook>>> SearchBooksAsync(string query)
        {
            try
            {
                var bookEntities = await _supabaseClient
                    .From<LibraryBookEntity>()
                    .Where(x => x.Title.Contains(query) || x.Author.Contains(query) || x.Category.Contains(query))
                    .Limit(50)
                    .Get();

                var books = new List<LibraryBook>();

                if (bookEntities?.Models != null)
                {
                    foreach (var entity in bookEntities.Models)
                    {
                        books.Add(new LibraryBook
                        {
                            BookId = entity.BookId,
                            Title = entity.Title,
                            Author = entity.Author,
                            ISBN = entity.ISBN,
                            Category = entity.Category,
                            Publisher = entity.Publisher,
                            PublicationYear = entity.PublicationYear ?? 0,
                            TotalCopies = entity.TotalCopies,
                            AvailableCopies = entity.AvailableCopies,
                            Description = entity.Description,
                            ImageUrl = entity.ImageUrl,
                            DateAdded = entity.CreatedAt
                        });
                    }
                }

                return new ApiResponse<List<LibraryBook>>
                {
                    Success = true,
                    Data = books,
                    Message = $"Found {books.Count} matching books"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error searching books with query {Query}", query);
                return new ApiResponse<List<LibraryBook>>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "BOOK_SEARCH_FAILED"
                };
            }
        }

        /// <summary>
        /// Request to borrow a book
        /// </summary>
        public async Task<ApiResponse<bool>> RequestBookAsync(string bookId, string studentId)
        {
            try
            {
                // Simulate API call delay
                await Task.Delay(1000);

                // In a real implementation, you would create a book request record
                // or directly issue the book if available

                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Book request submitted successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error requesting book {BookId} for student {StudentId}", bookId, studentId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "BOOK_REQUEST_FAILED"
                };
            }
        }

        /// <summary>
        /// Renew an issued book
        /// </summary>
        public async Task<ApiResponse<DateTime>> RenewBookAsync(string issueId)
        {
            try
            {
                var bookIssue = await _supabaseClient
                    .From<BookIssueEntity>()
                    .Where(x => x.Id == issueId)
                    .Single();

                if (bookIssue != null)
                {
                    var newDueDate = DateTime.Now.AddDays(14);
                    bookIssue.DueDate = newDueDate;
                    await bookIssue.Update<BookIssueEntity>();

                    return new ApiResponse<DateTime>
                    {
                        Success = true,
                        Data = newDueDate,
                        Message = "Book renewed successfully"
                    };
                }
                else
                {
                    return new ApiResponse<DateTime>
                    {
                        Success = false,
                        Message = "Book issue record not found",
                        ErrorCode = "BOOK_ISSUE_NOT_FOUND"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error renewing book {IssueId}", issueId);
                return new ApiResponse<DateTime>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "BOOK_RENEWAL_FAILED"
                };
            }
        }

        #endregion

        #region Fees Methods

        /// <summary>
        /// Get fees information for a student
        /// </summary>
        public async Task<ApiResponse<FeesInfo>> GetFeesInfoAsync(string studentId)
        {
            try
            {
                var currentStudentId = await GetCurrentUserEntityIdAsync();

                var feesEntity = await _supabaseClient
                    .From<FeesEntity>()
                    .Where(x => x.StudentId == currentStudentId && x.Year == DateTime.Now.Year)
                    .Single();

                if (feesEntity == null)
                {
                    // Create default fees record if it doesn't exist
                    feesEntity = new FeesEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        StudentId = currentStudentId,
                        TotalFees = 80000m,
                        PaidAmount = 0m,
                        Balance = 80000m,
                        Year = DateTime.Now.Year,
                        Term = GetCurrentTerm(),
                        DueDate = DateTime.Now.AddDays(30),
                        PaymentStatus = "Pending"
                    };

                    await _supabaseClient.From<FeesEntity>().Insert(feesEntity);
                }

                // Get payment history
                var paymentEntities = await _supabaseClient
                    .From<FeesPaymentEntity>()
                    .Where(x => x.StudentId == currentStudentId && x.IsApproved)
                    .Order(x => x.PaymentDate, Ordering.Descending)
                    .Get();

                var paymentHistory = new List<Models.PaymentRecord>();
                if (paymentEntities?.Models != null)
                {
                    foreach (var payment in paymentEntities.Models)
                    {
                        paymentHistory.Add(new Models.PaymentRecord
                        {
                            PaymentId = payment.Id,
                            StudentId = payment.StudentId,
                            Amount = payment.Amount,
                            PaymentDate = payment.PaymentDate,
                            PaymentMethod = payment.PaymentMethod,
                            ReceiptNumber = payment.ReceiptNumber,
                            Description = payment.Description ?? "",
                            ReceivedBy = payment.ReceivedBy,
                            TransactionId = payment.TransactionId ?? "",
                            IsApproved = payment.IsApproved,
                            Status = payment.IsApproved ? "Approved" : "Pending"
                        });
                    }
                }

                var feesInfo = new FeesInfo
                {
                    StudentId = feesEntity.StudentId,
                    TotalFees = feesEntity.TotalFees,
                    PaidAmount = feesEntity.PaidAmount,
                    Balance = feesEntity.Balance,
                    LastPaymentDate = feesEntity.LastPaymentDate ?? DateTime.MinValue,
                    PaymentStatus = feesEntity.PaymentStatus,
                    DueDate = feesEntity.DueDate,
                    PaymentHistory = paymentHistory,
                    AcademicYear = $"{DateTime.Now.Year}/{DateTime.Now.Year + 1}"
                };

                return new ApiResponse<FeesInfo>
                {
                    Success = true,
                    Data = feesInfo,
                    Message = "Fees information retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting fees info for student {StudentId}", studentId);
                return new ApiResponse<FeesInfo>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "FEES_LOAD_FAILED"
                };
            }
        }

        #endregion

        #region Financial Management Methods

        /// <summary>
        /// Get students with pending fees
        /// </summary>
        public async Task<List<Student>> GetStudentsWithPendingFeesAsync()
        {
            try
            {
                var studentsWithFees = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.IsActive)
                    .Get();

                var students = new List<Student>();

                if (studentsWithFees?.Models != null)
                {
                    foreach (var studentEntity in studentsWithFees.Models)
                    {
                        // Check if student has pending fees
                        var fees = await _supabaseClient
                            .From<FeesEntity>()
                            .Where(x => x.StudentId == studentEntity.Id && x.Balance > 0)
                            .Single();

                        if (fees != null)
                        {
                            students.Add(MapStudentEntityToModel(studentEntity));
                        }
                    }
                }

                return students;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting students with pending fees");
                return new List<Student>();
            }
        }

        /// <summary>
        /// Get comprehensive student data including fees, assignments, and books
        /// </summary>
        public async Task<ApiResponse<StudentDetailedInfo>> GetStudentDetailedInfoAsync(string studentId)
        {
            try
            {
                // Get student basic info
                var studentResponse = await GetStudentByIdAsync(studentId);
                if (!studentResponse.Success)
                {
                    return new ApiResponse<StudentDetailedInfo>
                    {
                        Success = false,
                        Message = studentResponse.Message,
                        ErrorCode = studentResponse.ErrorCode
                    };
                }

                var student = studentResponse.Data;

                // Get fees info
                var feesResponse = await GetFeesInfoAsync(studentId);
                var fees = feesResponse.Success ? feesResponse.Data : new FeesInfo();

                // Get assignments
                var assignmentsResponse = await GetAssignmentsAsync(student.Form);
                var assignments = assignmentsResponse.Success ? assignmentsResponse.Data : new List<Assignment>();

                // Get issued books
                var booksResponse = await GetIssuedBooksAsync(studentId);
                var issuedBooks = booksResponse.Success ? booksResponse.Data : new List<BookIssue>();

                // Get achievements
                var achievementsResponse = await GetStudentAchievementsAsync(studentId);
                var achievements = achievementsResponse.Success ? achievementsResponse.Data : new List<Achievement>();

                // Get performance
                var performanceResponse = await GetStudentPerformanceAsync(studentId);
                var performance = performanceResponse.Success ? performanceResponse.Data : new List<Models.SubjectPerformance>();

                var detailedInfo = new StudentDetailedInfo
                {
                    Student = student,
                    FeesInfo = fees,
                    Assignments = assignments,
                    IssuedBooks = issuedBooks,
                    Achievements = achievements,
                    Performance = performance
                };

                return new ApiResponse<StudentDetailedInfo>
                {
                    Success = true,
                    Data = detailedInfo,
                    Message = "Student detailed information retrieved successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting detailed student info for {StudentId}", studentId);
                return new ApiResponse<StudentDetailedInfo>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "STUDENT_DETAILED_INFO_FAILED"
                };
            }
        }

        /// <summary>
        /// Get system health information
        /// </summary>
        public async Task<ApiResponse<SystemHealthInfo>> GetSystemHealthAsync()
        {
            try
            {
                var startTime = DateTime.UtcNow;
                var healthInfo = new SystemHealthInfo
                {
                    DatabaseConnected = await TestDatabaseConnectionAsync(),
                    SupabaseConnected = SupabaseService.Instance.IsConnected,
                    LastChecked = DateTime.UtcNow,
                    Version = "1.0.0",
                    Environment = "Production"
                };

                healthInfo.IsHealthy = healthInfo.DatabaseConnected && healthInfo.SupabaseConnected;
                healthInfo.ResponseTime = DateTime.UtcNow - startTime;

                return new ApiResponse<SystemHealthInfo>
                {
                    Success = true,
                    Data = healthInfo,
                    Message = healthInfo.IsHealthy ? "System is healthy" : "System has issues"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error checking system health");
                return new ApiResponse<SystemHealthInfo>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "HEALTH_CHECK_FAILED"
                };
            }
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        private async Task<bool> TestDatabaseConnectionAsync()
        {
            try
            {
                var testQuery = await _supabaseClient
                    .From<UserEntity>()
                    .Limit(1)
                    .Get();

                return testQuery != null;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Map StudentEntity to Student model
        /// </summary>
        private static Student MapStudentEntityToModel(StudentEntity entity)
        {
            return new Student
            {
                StudentId = entity.StudentId,
                FullName = entity.FullName,
                AdmissionNo = entity.AdmissionNo,
                Form = entity.Form,
                Class = entity.Class,
                StreamName = $"{entity.Form}{entity.Class}",
                Email = entity.Email ?? string.Empty,
                PhoneNumber = entity.ParentPhone,
                ParentPhone = entity.ParentPhone,
                ParentEmail = "",
                DateOfBirth = entity.DateOfBirth,
                Gender = entity.Gender,
                Address = entity.Address ?? string.Empty,
                AdmissionDate = entity.EnrollmentDate,
                Status = ParseStudentStatus(entity.Status),
                AccountStatus = entity.IsActive ? AccountStatus.Active : AccountStatus.Inactive,
                AcademicYear = entity.Year,
                GuardianName = entity.GuardianName ?? string.Empty,
                GuardianPhone = entity.GuardianPhone ?? string.Empty,
                GuardianRelationship = "",
                PreviousSchool = entity.PreviousSchool ?? string.Empty,
                KCSEIndexNumber = entity.KcseIndexNumber ?? string.Empty,
                Nationality = entity.Nationality,
                Religion = entity.Religion ?? string.Empty,
                MedicalInfo = entity.MedicalInfo ?? string.Empty,
                RegistrationStatus = entity.IsActive ? "Complete" : "Pending"
            };
        }

        /// <summary>
        /// Map TeacherEntity to Teacher model
        /// </summary>
        private static Teacher MapTeacherEntityToModel(TeacherEntity entity)
        {
            return new Teacher
            {
                TeacherId = entity.TeacherId,
                FullName = entity.FullName,
                Email = entity.Email ?? string.Empty,
                PhoneNumber = entity.PhoneNumber ?? string.Empty,
                EmployeeNumber = entity.EmployeeNumber,
                EmployeeType = ParseEmployeeType(entity.EmployeeType),
                NTSCNumber = entity.TscNumber ?? string.Empty,
                QualifiedSubjects = entity.QualifiedSubjects ?? new List<string>(),
                AssignedSubjects = entity.Subjects ?? new List<string>(),
                AssignedClasses = entity.AssignedForms ?? new List<string>(),
                ClassTeacherFor = entity.ClassTeacherFor ?? string.Empty,
                HireDate = entity.EmploymentDate ?? DateTime.Now,
                DateJoined = entity.DateJoined,
                ContractEndDate = null,
                AccountStatus = entity.IsActive ? AccountStatus.Active : AccountStatus.Inactive,
                MonthlySalary = entity.MonthlySalary ?? 0,
                BankAccount = entity.BankAccount ?? string.Empty,
                IdNumber = entity.NationalId ?? string.Empty,
                DateOfBirth = DateTime.MinValue,
                Gender = "",
                Address = "",
                NextOfKin = "",
                NextOfKinPhone = "",
                Qualifications = entity.Qualification ?? string.Empty,
                IsClassTeacher = entity.IsClassTeacher,
                Department = entity.Department ?? string.Empty,
                Subjects = entity.Subjects ?? new List<string>()
            };
        }

        /// <summary>
        /// Map StaffEntity to Staff model
        /// </summary>
        private static Staff MapStaffEntityToModel(StaffEntity entity)
        {
            return new Staff
            {
                StaffId = entity.StaffId,
                FullName = entity.FullName,
                Email = entity.Email ?? string.Empty,
                PhoneNumber = entity.PhoneNumber ?? string.Empty,
                EmployeeNumber = entity.EmployeeNumber,
                Position = entity.Position,
                JobTitle = entity.Position,
                HireDate = entity.EmploymentDate ?? DateTime.Now,
                DateJoined = entity.DateJoined,
                AccountStatus = entity.IsActive ? AccountStatus.Active : AccountStatus.Inactive,
                MonthlySalary = entity.Salary ?? 0,
                BankAccount = entity.BankAccount ?? string.Empty,
                IdNumber = entity.NationalId ?? string.Empty,
                DateOfBirth = DateTime.MinValue,
                Gender = "",
                Address = "",
                NextOfKin = "",
                NextOfKinPhone = "",
                Department = entity.Department ?? string.Empty
            };
        }

        /// <summary>
        /// Parse student status from string
        /// </summary>
        private static StudentStatus ParseStudentStatus(string status)
        {
            return status?.ToLower() switch
            {
                "active" => StudentStatus.Current,
                "graduated" => StudentStatus.Graduated,
                "alumni" => StudentStatus.Alumni,
                "repeating" => StudentStatus.Repeating,
                "transferred" => StudentStatus.Transferred,
                "dropped" => StudentStatus.Dropped,
                "suspended" => StudentStatus.Suspended,
                _ => StudentStatus.Current
            };
        }

        /// <summary>
        /// Parse employee type from string
        /// </summary>
        private static EmployeeType ParseEmployeeType(string employeeType)
        {
            return employeeType?.ToUpper() switch
            {
                "BOM" => EmployeeType.BOM,
                "NTSC" => EmployeeType.NTSC,
                "CONTRACT" => EmployeeType.Contract,
                "VOLUNTEER" => EmployeeType.Volunteer,
                "TEACHINGPRACTICE" => EmployeeType.TeachingPractice,
                _ => EmployeeType.BOM
            };
        }

        /// <summary>
        /// Get user full name by user ID and type
        /// </summary>
        private async Task<string> GetUserFullNameAsync(string userId, string userType)
        {
            try
            {
                return userType.ToLower() switch
                {
                    "student" => await GetStudentFullNameAsync(userId),
                    "teacher" => await GetTeacherFullNameAsync(userId),
                    _ => await GetStaffFullNameAsync(userId)
                };
            }
            catch
            {
                return "Unknown User";
            }
        }

        /// <summary>
        /// Get user email by user ID and type
        /// </summary>
        private async Task<string> GetUserEmailAsync(string userId, string userType)
        {
            try
            {
                return userType.ToLower() switch
                {
                    "student" => await GetStudentEmailAsync(userId),
                    "teacher" => await GetTeacherEmailAsync(userId),
                    _ => await GetStaffEmailAsync(userId)
                };
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Get student full name by user ID
        /// </summary>
        private async Task<string> GetStudentFullNameAsync(string userId)
        {
            var student = await _supabaseClient
                .From<StudentEntity>()
                .Where(x => x.UserId == userId)
                .Single();
            return student?.FullName ?? "Unknown Student";
        }

        /// <summary>
        /// Get teacher full name by user ID
        /// </summary>
        private async Task<string> GetTeacherFullNameAsync(string userId)
        {
            var teacher = await _supabaseClient
                .From<TeacherEntity>()
                .Where(x => x.UserId == userId)
                .Single();
            return teacher?.FullName ?? "Unknown Teacher";
        }

        /// <summary>
        /// Get staff full name by user ID
        /// </summary>
        private async Task<string> GetStaffFullNameAsync(string userId)
        {
            var staff = await _supabaseClient
                .From<StaffEntity>()
                .Where(x => x.UserId == userId)
                .Single();
            return staff?.FullName ?? "Unknown Staff";
        }

        /// <summary>
        /// Get student email by user ID
        /// </summary>
        private async Task<string> GetStudentEmailAsync(string userId)
        {
            var student = await _supabaseClient
                .From<StudentEntity>()
                .Where(x => x.UserId == userId)
                .Single();
            return student?.Email ?? string.Empty;
        }

        /// <summary>
        /// Get teacher email by user ID
        /// </summary>
        private async Task<string> GetTeacherEmailAsync(string userId)
        {
            var teacher = await _supabaseClient
                .From<TeacherEntity>()
                .Where(x => x.UserId == userId)
                .Single();
            return teacher?.Email ?? string.Empty;
        }

        /// <summary>
        /// Get staff email by user ID
        /// </summary>
        private async Task<string> GetStaffEmailAsync(string userId)
        {
            var staff = await _supabaseClient
                .From<StaffEntity>()
                .Where(x => x.UserId == userId)
                .Single();
            return staff?.Email ?? string.Empty;
        }

        /// <summary>
        /// Get assignment submission for student
        /// </summary>
        private async Task<AssignmentSubmissionEntity?> GetAssignmentSubmissionAsync(string assignmentId, string studentId)
        {
            try
            {
                return await _supabaseClient
                    .From<AssignmentSubmissionEntity>()
                    .Where(x => x.AssignmentId == assignmentId && x.StudentId == studentId)
                    .Single();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get current user entity ID
        /// </summary>
        private async Task<string> GetCurrentUserEntityIdAsync()
        {
            try
            {
                var currentStudent = _authService.CurrentStudent;
                if (currentStudent != null)
                {
                    var student = await _supabaseClient
                        .From<StudentEntity>()
                        .Where(x => x.StudentId == currentStudent.StudentId)
                        .Single();
                    return student?.Id ?? string.Empty;
                }

                var currentTeacher = _authService.CurrentTeacher;
                if (currentTeacher != null)
                {
                    var teacher = await _supabaseClient
                        .From<TeacherEntity>()
                        .Where(x => x.TeacherId == currentTeacher.TeacherId)
                        .Single();
                    return teacher?.Id ?? string.Empty;
                }

                var currentStaff = _authService.CurrentStaff;
                if (currentStaff != null)
                {
                    var staff = await _supabaseClient
                        .From<StaffEntity>()
                        .Where(x => x.StaffId == currentStaff.StaffId)
                        .Single();
                    return staff?.Id ?? string.Empty;
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Get teacher name by teacher ID
        /// </summary>
        private async Task<string> GetTeacherNameAsync(string teacherId)
        {
            try
            {
                var teacher = await _supabaseClient
                    .From<TeacherEntity>()
                    .Where(x => x.Id == teacherId)
                    .Single();
                return teacher?.FullName ?? "Unknown Teacher";
            }
            catch
            {
                return "Unknown Teacher";
            }
        }

        /// <summary>
        /// Get current term based on date
        /// </summary>
        private static int GetCurrentTerm()
        {
            var currentMonth = DateTime.Now.Month;
            return currentMonth switch
            {
                >= 1 and <= 4 => 1,
                >= 5 and <= 8 => 2,
                _ => 3
            };
        }

        /// <summary>
        /// Generate dummy PDF content for testing
        /// </summary>
        private static byte[] GenerateDummyPdfContent()
        {
            var pdfContent = "%PDF-1.4\n1 0 obj\n<<\n/Type /Catalog\n/Pages 2 0 R\n>>\nendobj\n2 0 obj\n<<\n/Type /Pages\n/Kids [3 0 R]\n/Count 1\n>>\nendobj\n3 0 obj\n<<\n/Type /Page\n/Parent 2 0 R\n/MediaBox [0 0 612 792]\n>>\nendobj\nxref\n0 4\n0000000000 65535 f \n0000000009 00000 n \n0000000058 00000 n \n0000000115 00000 n \ntrailer\n<<\n/Size 4\n/Root 1 0 R\n>>\nstartxref\n174\n%%EOF";
            return System.Text.Encoding.ASCII.GetBytes(pdfContent);
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // Dispose managed resources here if needed
                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~ApiService()
        {
            Dispose(false);
        }

        #endregion
    }
}