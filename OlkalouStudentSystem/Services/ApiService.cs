// ===============================
// Services/ApiService.cs - Complete API Service with Supabase Integration
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Models.Data;
using System.Text.Json;

namespace OlkalouStudentSystem.Services
{
    public class ApiService
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly AuthService _authService;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<ApiService>? _logger;

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

        #region Authentication

        /// <summary>
        /// This method is now handled by AuthService directly
        /// Kept for compatibility with existing ViewModels
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
                if (success)
                {
                    return new ApiResponse<string>
                    {
                        Success = true,
                        Data = _authService.AuthToken,
                        Message = "Token refreshed successfully"
                    };
                }
                else
                {
                    return new ApiResponse<string>
                    {
                        Success = false,
                        Message = "Token refresh failed",
                        ErrorCode = "TOKEN_REFRESH_FAILED"
                    };
                }
            }
            catch (Exception ex)
            {
                return new ApiResponse<string>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "TOKEN_REFRESH_FAILED"
                };
            }
        }

        #endregion

        #region Notification Management

        /// <summary>
        /// Create a new notification
        /// </summary>
        public async Task<ApiResponse<bool>> CreateNotificationAsync(Models.Notification notification)
        {
            try
            {
                var notificationEntity = new NotificationEntity
                {
                    Id = notification.NotificationId,
                    Title = notification.Title,
                    Message = notification.Message,
                    RecipientId = notification.RecipientId,
                    NotificationType = notification.NotificationType,
                    Priority = notification.Priority,
                    ActionUrl = notification.ActionUrl,
                    ExpiryDate = notification.ExpiryDate,
                    IsRead = notification.IsRead,
                    CreatedBy = notification.CreatedBy,
                    CreatedAt = notification.CreatedDate
                };

                await _supabaseClient.From<NotificationEntity>().Insert(notificationEntity);

                return new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Notification created successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating notification");
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = ex.Message,
                    ErrorCode = "NOTIFICATION_CREATE_FAILED"
                };
            }
        }

        /// <summary>
        /// Update an existing notification
        /// </summary>
        public async Task<ApiResponse<bool>> UpdateNotificationAsync(Models.Notification notification)
        {
            try
            {
                var notificationEntity = await _supabaseClient
                    .From<NotificationEntity>()
                    .Where(x => x.Id == notification.NotificationId)
                    .Single();

                if (notificationEntity != null)
                {
                    notificationEntity.Title = notification.Title;
                    notificationEntity.Message = notification.Message;
                    notificationEntity.IsRead = notification.IsRead;
                    notificationEntity.UpdatedAt = DateTime.UtcNow;

                    await notificationEntity.Update<NotificationEntity>();

                    return new ApiResponse<bool>
                    {
                        Success = true,
                        Data = true,
                        Message = "Notification updated successfully"
                    };
                }
                else
                {
                    return new ApiResponse<bool>
                    {
                        Success = false,
                        Data = false,
                        Message = "Notification not found",
                        ErrorCode = "NOTIFICATION_NOT_FOUND"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating notification {NotificationId}", notification.NotificationId);
                return new ApiResponse<bool>
                {
                    Success = false,
                    Data = false,
                    Message = ex.Message,
                    ErrorCode = "NOTIFICATION_UPDATE_FAILED"
                };
            }
        }

        /// <summary>
        /// Mark all notifications as read for a user
        /// </summary>
        public async Task<bool> MarkAllNotificationsAsReadAsync(string userId)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(currentUserId))
                    return false;

                var notifications = await _supabaseClient
                    .From<NotificationEntity>()
                    .Where(x => x.RecipientId == currentUserId && !x.IsRead)
                    .Get();

                if (notifications?.Models != null)
                {
                    foreach (var notification in notifications.Models)
                    {
                        notification.IsRead = true;
                        notification.UpdatedAt = DateTime.UtcNow;
                        await notification.Update<NotificationEntity>();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error marking all notifications as read");
                return false;
            }
        }

        /// <summary>
        /// Delete notification
        /// </summary>
        public async Task<bool> DeleteNotificationAsync(string notificationId)
        {
            try
            {
                var notification = await _supabaseClient
                    .From<NotificationEntity>()
                    .Where(x => x.Id == notificationId)
                    .Single();

                if (notification != null)
                {
                    await notification.Delete<NotificationEntity>();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting notification {NotificationId}", notificationId);
                return false;
            }
        }

        /// <summary>
        /// Delete all notifications for current user
        /// </summary>
        public async Task<bool> DeleteAllNotificationsAsync(string userId)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                if (string.IsNullOrEmpty(currentUserId))
                    return false;

                var notifications = await _supabaseClient
                    .From<NotificationEntity>()
                    .Where(x => x.RecipientId == currentUserId)
                    .Get();

                if (notifications?.Models != null)
                {
                    foreach (var notification in notifications.Models)
                    {
                        await notification.Delete<NotificationEntity>();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error deleting all notifications");
                return false;
            }
        }

        /// <summary>
        /// Cleanup expired notifications
        /// </summary>
        public async Task<bool> CleanupExpiredNotificationsAsync()
        {
            try
            {
                var expiredNotifications = await _supabaseClient
                    .From<NotificationEntity>()
                    .Where(x => x.ExpiryDate.HasValue && x.ExpiryDate < DateTime.UtcNow)
                    .Get();

                if (expiredNotifications?.Models != null)
                {
                    foreach (var notification in expiredNotifications.Models)
                    {
                        await notification.Delete<NotificationEntity>();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error cleaning up expired notifications");
                return false;
            }
        }

        #endregion

        #region User Management

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
                        users.Add(new UserProfile
                        {
                            UserId = userEntity.Id,
                            PhoneNumber = userEntity.PhoneNumber,
                            UserType = userType,
                            FullName = await GetUserFullNameAsync(userEntity.Id, userTypeString),
                            Email = await GetUserEmailAsync(userEntity.Id, userTypeString)
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
                        students.Add(new Student
                        {
                            StudentId = studentEntity.StudentId,
                            FullName = studentEntity.FullName,
                            AdmissionNo = studentEntity.AdmissionNo,
                            Form = studentEntity.Form,
                            Class = studentEntity.Class,
                            Email = studentEntity.Email,
                            PhoneNumber = studentEntity.ParentPhone,
                            DateOfBirth = studentEntity.DateOfBirth,
                            Gender = studentEntity.Gender,
                            Address = studentEntity.Address
                        });
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
                        students.Add(new Student
                        {
                            StudentId = studentEntity.StudentId,
                            FullName = studentEntity.FullName,
                            AdmissionNo = studentEntity.AdmissionNo,
                            Form = studentEntity.Form,
                            Class = studentEntity.Class,
                            Email = studentEntity.Email,
                            PhoneNumber = studentEntity.ParentPhone,
                            DateOfBirth = studentEntity.DateOfBirth,
                            Gender = studentEntity.Gender,
                            Address = studentEntity.Address
                        });
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

        #region Assignments

        /// <summary>
        /// Get assignments for a specific form/class from Supabase
        /// </summary>
        public async Task<ApiResponse<List<Assignment>>> GetAssignmentsAsync(string form)
        {
            try
            {
                var assignmentEntities = await _supabaseClient
                    .From<AssignmentEntity>()
                    .Where(x => x.Form == form)
                    .Order(x => x.DueDate, Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Get();

                var assignments = new List<Assignment>();

                if (assignmentEntities?.Models != null)
                {
                    foreach (var entity in assignmentEntities.Models)
                    {
                        // Check if current student has submitted this assignment
                        var currentStudentId = await GetCurrentStudentIdAsync();
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
                var currentStudentId = await GetCurrentStudentIdAsync();
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
                // For now, we'll simulate the upload and store the submission record
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
        /// Download assignment file (simulated)
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

        #region Activities

        /// <summary>
        /// Get school activities from Supabase
        /// </summary>
        public async Task<ApiResponse<List<Activity>>> GetActivitiesAsync(string form)
        {
            try
            {
                var activityEntities = await _supabaseService.Client
                    .From<ActivityEntity>()
                    .Where(x => x.TargetForms.Contains(form) || x.TargetForms.Contains("All"))
                    .Order(x => x.Date, Supabase.Postgrest.Constants.Ordering.Ascending)
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
                            Status = entity.Status
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
        /// Register for an activity (simulated - would need activity registration table)
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

        #region Library

        /// <summary>
        /// Get books issued to a student from Supabase
        /// </summary>
        public async Task<ApiResponse<List<BookIssue>>> GetIssuedBooksAsync(string studentId)
        {
            try
            {
                var currentStudentId = await GetCurrentStudentIdAsync();

                var bookIssueEntities = await _supabaseClient
                    .From<BookIssueEntity>()
                    .Where(x => x.StudentId == currentStudentId && x.Status == "Issued")
                    .Order(x => x.IssueDate, Postgrest.Constants.Ordering.Descending)
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
        /// Request to borrow a book (simulated)
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

        #region Fees

        /// <summary>
        /// Get fees information for a student from Supabase
        /// </summary>
        public async Task<ApiResponse<FeesInfo>> GetFeesInfoAsync(string studentId)
        {
            try
            {
                var currentStudentId = await GetCurrentStudentIdAsync();

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
                    .Order(x => x.PaymentDate, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                var paymentHistory = new List<PaymentRecord>();
                if (paymentEntities?.Models != null)
                {
                    foreach (var payment in paymentEntities.Models)
                    {
                        paymentHistory.Add(new PaymentRecord
                        {
                            PaymentId = payment.Id,
                            Amount = payment.Amount,
                            PaymentDate = payment.PaymentDate,
                            PaymentMethod = payment.PaymentMethod,
                            ReceiptNumber = payment.ReceiptNumber,
                            Description = payment.Description ?? "",
                            ReceivedBy = payment.ReceivedBy
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
                    PaymentHistory = paymentHistory
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

        /// <summary>
        /// Process a fee payment
        /// </summary>
        public async Task<ApiResponse<PaymentRecord>> MakePaymentAsync(string studentId, decimal amount, string paymentMethod)
        {
            try
            {
                var currentStudentId = await GetCurrentStudentIdAsync();

                // Create payment record
                var payment = new FeesPaymentEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = currentStudentId,
                    Amount = amount,
                    PaymentMethod = paymentMethod,
                    ReceiptNumber = GenerateReceiptNumber(),
                    IsApproved = true, // Auto-approve for mobile payments
                    Description = "Fees Payment via Mobile App",
                    ReceivedBy = "Mobile App System",
                    PaymentDate = DateTime.UtcNow,
                    VerifiedBy = "system",
                    VerificationDate = DateTime.UtcNow
                };

                await _supabaseClient.From<FeesPaymentEntity>().Insert(payment);

                // Update fees balance
                await UpdateFeesBalanceAsync(currentStudentId, amount);

                var paymentRecord = new PaymentRecord
                {
                    PaymentId = payment.Id,
                    Amount = payment.Amount,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.PaymentMethod,
                    ReceiptNumber = payment.ReceiptNumber,
                    Description = payment.Description,
                    ReceivedBy = payment.ReceivedBy
                };

                return new ApiResponse<PaymentRecord>
                {
                    Success = true,
                    Data = paymentRecord,
                    Message = "Payment processed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing payment for student {StudentId}", studentId);
                return new ApiResponse<PaymentRecord>
                {
                    Success = false,
                    Message = ex.Message,
                    ErrorCode = "PAYMENT_FAILED"
                };
            }
        }

        #endregion

        #region Dashboard

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

                var dashboardData = new DashboardData
                {
                    CurrentFees = feesInfo,
                    PendingAssignments = new System.Collections.ObjectModel.ObservableCollection<Assignment>(pendingAssignments),
                    IssuedBooks = new System.Collections.ObjectModel.ObservableCollection<LibraryBook>(),
                    RecentAchievements = new System.Collections.ObjectModel.ObservableCollection<Achievement>(),
                    UpcomingActivities = new System.Collections.ObjectModel.ObservableCollection<Activity>(upcomingActivities),
                    AcademicPerformance = new System.Collections.ObjectModel.ObservableCollection<SubjectPerformance>(GenerateDemoPerformance())
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

        #endregion

        #region Financial Management (New Methods)

        /// <summary>
        /// Get all invoices
        /// </summary>
        public async Task<List<Invoice>> GetInvoicesAsync()
        {
            try
            {
                // Return demo data for now - implement Supabase integration as needed
                return new List<Invoice>
                {
                    new Invoice
                    {
                        InvoiceId = "INV001",
                        InvoiceNumber = "INV-2024-001",
                        VendorId = "VEN001",
                        VendorName = "ABC Supplies",
                        Amount = 15000,
                        Status = "Pending",
                        Description = "Office supplies",
                        DueDate = DateTime.Now.AddDays(30),
                        CreatedAt = DateTime.Now
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting invoices");
                return new List<Invoice>();
            }
        }

        /// <summary>
        /// Get pending invoices
        /// </summary>
        public async Task<List<Invoice>> GetPendingInvoicesAsync()
        {
            try
            {
                var allInvoices = await GetInvoicesAsync();
                return allInvoices.Where(i => i.Status == "Pending").ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting pending invoices");
                return new List<Invoice>();
            }
        }

        /// <summary>
        /// Update invoice
        /// </summary>
        public async Task<bool> UpdateInvoiceAsync(Invoice invoice)
        {
            try
            {
                // Simulate update operation
                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating invoice {InvoiceId}", invoice?.InvoiceId);
                return false;
            }
        }

        /// <summary>
        /// Get salary payments
        /// </summary>
        public async Task<List<SalaryPayment>> GetSalaryPaymentsAsync()
        {
            try
            {
                // Return demo data for now
                return new List<SalaryPayment>
                {
                    new SalaryPayment
                    {
                        PaymentId = "SAL001",
                        EmployeeId = "EMP001",
                        EmployeeName = "John Doe",
                        BasicSalary = 50000,
                        Allowances = 10000,
                        Deductions = 5000,
                        NetSalary = 55000,
                        PayrollMonth = "December",
                        PayrollYear = 2024,
                        Status = "Pending",
                        PaymentDate = DateTime.Now
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting salary payments");
                return new List<SalaryPayment>();
            }
        }

        /// <summary>
        /// Get pending salary payments
        /// </summary>
        public async Task<List<SalaryPayment>> GetPendingSalaryPaymentsAsync()
        {
            try
            {
                var allSalaries = await GetSalaryPaymentsAsync();
                return allSalaries.Where(s => s.Status == "Pending").ToList();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting pending salary payments");
                return new List<SalaryPayment>();
            }
        }

        /// <summary>
        /// Update salary payment
        /// </summary>
        public async Task<bool> UpdateSalaryPaymentAsync(SalaryPayment payment)
        {
            try
            {
                // Simulate update operation
                await Task.Delay(500);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating salary payment {PaymentId}", payment?.PaymentId);
                return false;
            }
        }

        /// <summary>
        /// Get journal entries
        /// </summary>
        public async Task<List<JournalEntry>> GetJournalEntriesAsync()
        {
            try
            {
                // Return demo data for now
                return new List<JournalEntry>
                {
                    new JournalEntry
                    {
                        EntryId = "JE001",
                        Date = DateTime.Now,
                        Description = "School fees collection",
                        Reference = "REF001",
                        DebitAccount = "Cash",
                        CreditAccount = "Fees Income",
                        Amount = 100000,
                        CreatedBy = "Bursar"
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting journal entries");
                return new List<JournalEntry>();
            }
        }

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
                            students.Add(new Student
                            {
                                StudentId = studentEntity.StudentId,
                                FullName = studentEntity.FullName,
                                AdmissionNo = studentEntity.AdmissionNo,
                                Form = studentEntity.Form,
                                Class = studentEntity.Class,
                                PhoneNumber = studentEntity.ParentPhone
                            });
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

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get current student ID from the authenticated user
        /// </summary>
        private async Task<string> GetCurrentStudentIdAsync()
        {
            try
            {
                var userId = await SecureStorage.GetAsync("user_id");
                if (string.IsNullOrEmpty(userId))
                    return null;

                var userType = await SecureStorage.GetAsync("user_type");

                switch (userType)
                {
                    case "Student":
                        var student = await _supabaseClient
                            .From<StudentEntity>()
                            .Where(x => x.UserId == userId)
                            .Single();
                        return student?.Id;

                    case "Teacher":
                        var teacher = await _supabaseClient
                            .From<TeacherEntity>()
                            .Where(x => x.UserId == userId)
                            .Single();
                        return teacher?.Id;

                    default:
                        var staff = await _supabaseClient
                            .From<StaffEntity>()
                            .Where(x => x.UserId == userId)
                            .Single();
                        return staff?.Id;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting current student ID");
                return null;
            }
        }

        /// <summary>
        /// Get assignment submission for a student
        /// </summary>
        private async Task<AssignmentSubmissionEntity> GetAssignmentSubmissionAsync(string assignmentId, string studentId)
        {
            try
            {
                if (string.IsNullOrEmpty(studentId))
                    return null;

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
        /// Get teacher name by ID
        /// </summary>
        private async Task<string> GetTeacherNameAsync(string teacherId)
        {
            try
            {
                var teacher = await _supabaseClient
                    .From<TeacherEntity>()
                    .Where(x => x.Id == teacherId)
                    .Single();

                return teacher?.FullName ?? "Teacher";
            }
            catch
            {
                return "Teacher";
            }
        }

        /// <summary>
        /// Get user full name by ID and type
        /// </summary>
        private async Task<string> GetUserFullNameAsync(string userId, string userType)
        {
            try
            {
                switch (userType)
                {
                    case "Student":
                        var student = await _supabaseClient
                            .From<StudentEntity>()
                            .Where(x => x.UserId == userId)
                            .Single();
                        return student?.FullName ?? "Student";

                    case "Teacher":
                        var teacher = await _supabaseClient
                            .From<TeacherEntity>()
                            .Where(x => x.UserId == userId)
                            .Single();
                        return teacher?.FullName ?? "Teacher";

                    default:
                        var staff = await _supabaseClient
                            .From<StaffEntity>()
                            .Where(x => x.UserId == userId)
                            .Single();
                        return staff?.FullName ?? "Staff";
                }
            }
            catch
            {
                return "User";
            }
        }

        /// <summary>
        /// Get user email by ID and type
        /// </summary>
        private async Task<string> GetUserEmailAsync(string userId, string userType)
        {
            try
            {
                switch (userType)
                {
                    case "Student":
                        var student = await _supabaseClient
                            .From<StudentEntity>()
                            .Where(x => x.UserId == userId)
                            .Single();
                        return student?.Email ?? "";

                    case "Teacher":
                        var teacher = await _supabaseClient
                            .From<TeacherEntity>()
                            .Where(x => x.UserId == userId)
                            .Single();
                        return teacher?.Email ?? "";

                    default:
                        var staff = await _supabaseClient
                            .From<StaffEntity>()
                            .Where(x => x.UserId == userId)
                            .Single();
                        return staff?.Email ?? "";
                }
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Update fees balance after payment
        /// </summary>
        private async Task UpdateFeesBalanceAsync(string studentId, decimal paidAmount)
        {
            try
            {
                var fees = await _supabaseClient
                    .From<FeesEntity>()
                    .Where(x => x.StudentId == studentId && x.Year == DateTime.Now.Year)
                    .Single();

                if (fees != null)
                {
                    fees.PaidAmount += paidAmount;
                    fees.Balance = fees.TotalFees - fees.PaidAmount;
                    fees.LastPaymentDate = DateTime.UtcNow;
                    fees.PaymentStatus = fees.Balance <= 0 ? "Paid" : "Pending";

                    await fees.Update<FeesEntity>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating fees balance for student {StudentId}", studentId);
            }
        }

        /// <summary>
        /// Generate receipt number
        /// </summary>
        private string GenerateReceiptNumber()
        {
            return $"RCP{DateTime.Now:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// Get current academic term
        /// </summary>
        private int GetCurrentTerm()
        {
            var month = DateTime.Now.Month;
            return month switch
            {
                >= 1 and <= 4 => 1,
                >= 5 and <= 8 => 2,
                >= 9 and <= 12 => 3,
                _ => 1
            };
        }

        /// <summary>
        /// Generate demo performance data (until real data is available)
        /// </summary>
        private List<SubjectPerformance> GenerateDemoPerformance()
        {
            return new List<SubjectPerformance>
            {
                new SubjectPerformance { Subject = "Mathematics", CurrentGrade = 85, PreviousGrade = 78, Trend = "up" },
                new SubjectPerformance { Subject = "English", CurrentGrade = 92, PreviousGrade = 89, Trend = "up" },
                new SubjectPerformance { Subject = "Science", CurrentGrade = 78, PreviousGrade = 82, Trend = "down" },
                new SubjectPerformance { Subject = "History", CurrentGrade = 89, PreviousGrade = 85, Trend = "up" },
                new SubjectPerformance { Subject = "Geography", CurrentGrade = 83, PreviousGrade = 80, Trend = "up" }
            };
        }

        /// <summary>
        /// Generate dummy PDF content for downloads
        /// </summary>
        private byte[] GenerateDummyPdfContent()
        {
            return System.Text.Encoding.UTF8.GetBytes("%PDF-1.4\nDummy PDF content for demo purposes");
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

    #region Supporting Model Classes

    public class User
    {
        public string UserId { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserProfile
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public Models.UserType UserType { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class Invoice
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

    public class SalaryPayment
    {
        public string PaymentId { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public decimal BasicSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
        public string PayrollMonth { get; set; } = string.Empty;
        public int PayrollYear { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string PaidBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
    }

    public class JournalEntry
    {
        public string EntryId { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string DebitAccount { get; set; } = string.Empty;
        public string CreditAccount { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // Missing Entity classes for notifications
    public class NotificationEntity : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id")]
        public string Id { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("title")]
        public string Title { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("message")]
        public string Message { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("recipient_id")]
        public string RecipientId { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("notification_type")]
        public string NotificationType { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("priority")]
        public string Priority { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("action_url")]
        public string? ActionUrl { get; set; }

        [Supabase.Postgrest.Attributes.Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [Supabase.Postgrest.Attributes.Column("is_read")]
        public bool IsRead { get; set; }

        [Supabase.Postgrest.Attributes.Column("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Supabase.Postgrest.Attributes.Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    #endregion
}