// ===============================
// Services/UserRegistrationService.cs - Complete User Registration Service
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OlkalouStudentSystem.Services
{
    public class UserRegistrationService
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly AuthService _authService;
        private readonly ILogger<UserRegistrationService>? _logger;

        public UserRegistrationService(AuthService authService, ILogger<UserRegistrationService>? logger = null)
        {
            _supabaseClient = SupabaseService.Instance.Client;
            _authService = authService;
            _logger = logger;
        }

        #region Student Registration

        /// <summary>
        /// Generate unique student ID in format GRS/YYYY/XXX
        /// </summary>
        public async Task<string> GenerateStudentIdAsync()
        {
            try
            {
                var year = DateTime.Now.Year;
                var prefix = $"GRS/{year}/";

                // Get the last student ID for current year
                var lastStudent = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.StudentId.StartsWith(prefix))
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                var nextNumber = 1;
                if (lastStudent?.Models?.Count > 0)
                {
                    var lastId = lastStudent.Models[0].StudentId;
                    var parts = lastId.Split('/');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var num))
                    {
                        nextNumber = num + 1;
                    }
                }

                return $"GRS/{year}/{nextNumber:D3}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating student ID");
                // Fallback to timestamp-based ID
                return $"GRS/{DateTime.Now.Year}/{DateTime.Now:HHmmss}";
            }
        }

        /// <summary>
        /// Register a new student (Secretary only)
        /// </summary>
        public async Task<RegistrationResult> RegisterStudentAsync(StudentRegistrationRequest request)
        {
            try
            {
                // Validate request
                var validationResult = ValidateStudentRequest(request);
                if (!validationResult.IsValid)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = validationResult.ErrorMessage
                    };
                }

                // Check if secretary is authenticated and authorized
                if (!await IsSecretaryAuthorizedAsync())
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "Unauthorized: Only secretary can register students"
                    };
                }

                // Check if phone number already exists
                var existingUser = await _supabaseClient
                    .From<UserEntity>()
                    .Where(x => x.PhoneNumber == request.ParentPhone)
                    .Single();

                if (existingUser != null)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "Phone number already registered"
                    };
                }

                // Check if admission number already exists
                var existingStudent = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.AdmissionNo == request.AdmissionNo)
                    .Single();

                if (existingStudent != null)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "Admission number already exists"
                    };
                }

                // Generate student ID and credentials
                var studentId = await GenerateStudentIdAsync();
                var password = studentId; // Student ID is the default password

                // Create user account
                var userEntity = new UserEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    PhoneNumber = request.ParentPhone,
                    Password = password,
                    UserType = "Student",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = await GetCurrentUserIdAsync()
                };

                await _supabaseClient.From<UserEntity>().Insert(userEntity);

                // Create student profile
                var studentEntity = new StudentEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userEntity.Id,
                    StudentId = studentId,
                    AdmissionNo = request.AdmissionNo,
                    FullName = request.FullName,
                    Form = request.Form,
                    Class = request.Class,
                    Email = request.Email,
                    DateOfBirth = request.DateOfBirth,
                    Gender = request.Gender,
                    Address = request.Address,
                    ParentPhone = request.ParentPhone,
                    Year = DateTime.Now.Year,
                    Term = GetCurrentTerm(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = await GetCurrentUserIdAsync()
                };

                await _supabaseClient.From<StudentEntity>().Insert(studentEntity);

                // Create default fees record
                await CreateDefaultFeesRecordAsync(studentEntity.Id);

                _logger?.LogInformation("Student registered successfully: {StudentId}", studentId);

                return new RegistrationResult
                {
                    Success = true,
                    Message = "Student registered successfully",
                    GeneratedId = studentId,
                    LoginCredentials = new LoginCredentials
                    {
                        PhoneNumber = request.ParentPhone,
                        Password = password
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Student registration error");
                return new RegistrationResult
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        #endregion

        #region Teacher Registration

        /// <summary>
        /// Generate unique teacher ID in format TCH/YYYY/XXX
        /// </summary>
        public async Task<string> GenerateTeacherIdAsync()
        {
            try
            {
                var year = DateTime.Now.Year;
                var prefix = $"TCH/{year}/";

                var lastTeacher = await _supabaseClient
                    .From<TeacherEntity>()
                    .Where(x => x.TeacherId.StartsWith(prefix))
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                var nextNumber = 1;
                if (lastTeacher?.Models?.Count > 0)
                {
                    var lastId = lastTeacher.Models[0].TeacherId;
                    var parts = lastId.Split('/');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var num))
                    {
                        nextNumber = num + 1;
                    }
                }

                return $"TCH/{year}/{nextNumber:D3}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating teacher ID");
                return $"TCH/{DateTime.Now.Year}/{DateTime.Now:HHmmss}";
            }
        }

        /// <summary>
        /// Register a new teacher (Secretary only)
        /// </summary>
        public async Task<RegistrationResult> RegisterTeacherAsync(TeacherRegistrationRequest request)
        {
            try
            {
                // Validate request
                var validationResult = ValidateTeacherRequest(request);
                if (!validationResult.IsValid)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = validationResult.ErrorMessage
                    };
                }

                // Check authorization
                if (!await IsSecretaryAuthorizedAsync())
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "Unauthorized: Only secretary can register teachers"
                    };
                }

                // Check if phone number already exists
                var existingUser = await _supabaseClient
                    .From<UserEntity>()
                    .Where(x => x.PhoneNumber == request.PhoneNumber)
                    .Single();

                if (existingUser != null)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "Phone number already registered"
                    };
                }

                // Check TSC number uniqueness if provided
                if (!string.IsNullOrEmpty(request.TscNumber))
                {
                    var existingTeacher = await _supabaseClient
                        .From<TeacherEntity>()
                        .Where(x => x.TscNumber == request.TscNumber)
                        .Single();

                    if (existingTeacher != null)
                    {
                        return new RegistrationResult
                        {
                            Success = false,
                            Message = "TSC number already exists"
                        };
                    }
                }

                // Generate teacher ID and credentials
                var teacherId = await GenerateTeacherIdAsync();
                var password = "teacher123"; // Default password - should be changed on first login

                // Create user account
                var userEntity = new UserEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    PhoneNumber = request.PhoneNumber,
                    Password = password,
                    UserType = "Teacher",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = await GetCurrentUserIdAsync()
                };

                await _supabaseClient.From<UserEntity>().Insert(userEntity);

                // Create teacher profile
                var teacherEntity = new TeacherEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userEntity.Id,
                    TeacherId = teacherId,
                    FullName = request.FullName,
                    EmployeeType = request.EmployeeType,
                    TscNumber = request.TscNumber,
                    NtscPayment = request.NtscPayment,
                    Subjects = request.Subjects ?? new List<string>(),
                    AssignedForms = request.AssignedForms ?? new List<string>(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = await GetCurrentUserIdAsync()
                };

                await _supabaseClient.From<TeacherEntity>().Insert(teacherEntity);

                _logger?.LogInformation("Teacher registered successfully: {TeacherId}", teacherId);

                return new RegistrationResult
                {
                    Success = true,
                    Message = "Teacher registered successfully",
                    GeneratedId = teacherId,
                    LoginCredentials = new LoginCredentials
                    {
                        PhoneNumber = request.PhoneNumber,
                        Password = password
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Teacher registration error");
                return new RegistrationResult
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        #endregion

        #region Staff Registration

        /// <summary>
        /// Generate unique staff ID based on position
        /// </summary>
        public async Task<string> GenerateStaffIdAsync(string position)
        {
            try
            {
                var year = DateTime.Now.Year;
                var prefix = position switch
                {
                    "Principal" => $"PRC/{year}/",
                    "DeputyPrincipal" => $"DPR/{year}/",
                    "Secretary" => $"SEC/{year}/",
                    "Bursar" => $"BUR/{year}/",
                    _ => $"STF/{year}/"
                };

                var lastStaff = await _supabaseClient
                    .From<StaffEntity>()
                    .Where(x => x.StaffId.StartsWith(prefix))
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();

                var nextNumber = 1;
                if (lastStaff?.Models?.Count > 0)
                {
                    var lastId = lastStaff.Models[0].StaffId;
                    var parts = lastId.Split('/');
                    if (parts.Length == 3 && int.TryParse(parts[2], out var num))
                    {
                        nextNumber = num + 1;
                    }
                }

                return prefix + $"{nextNumber:D3}";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating staff ID");
                var fallbackPrefix = position.Length >= 3 ? position.Substring(0, 3).ToUpper() : position.ToUpper();
                return $"{fallbackPrefix}/{DateTime.Now.Year}/{DateTime.Now:HHmmss}";
            }
        }

        /// <summary>
        /// Register new staff member (Secretary only)
        /// </summary>
        public async Task<RegistrationResult> RegisterStaffAsync(StaffRegistrationRequest request)
        {
            try
            {
                // Validate request
                var validationResult = ValidateStaffRequest(request);
                if (!validationResult.IsValid)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = validationResult.ErrorMessage
                    };
                }

                // Check authorization
                if (!await IsSecretaryAuthorizedAsync())
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "Unauthorized: Only secretary can register staff"
                    };
                }

                // Check if phone number already exists
                var existingUser = await _supabaseClient
                    .From<UserEntity>()
                    .Where(x => x.PhoneNumber == request.PhoneNumber)
                    .Single();

                if (existingUser != null)
                {
                    return new RegistrationResult
                    {
                        Success = false,
                        Message = "Phone number already registered"
                    };
                }

                // Generate staff ID and credentials
                var staffId = await GenerateStaffIdAsync(request.Position);
                var password = "staff123"; // Default password

                // Create user account
                var userEntity = new UserEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    PhoneNumber = request.PhoneNumber,
                    Password = password,
                    UserType = request.Position,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = await GetCurrentUserIdAsync()
                };

                await _supabaseClient.From<UserEntity>().Insert(userEntity);

                // Create staff profile
                var staffEntity = new StaffEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userEntity.Id,
                    StaffId = staffId,
                    FullName = request.FullName,
                    Position = request.Position,
                    Department = request.Department,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = await GetCurrentUserIdAsync()
                };

                await _supabaseClient.From<StaffEntity>().Insert(staffEntity);

                _logger?.LogInformation("Staff registered successfully: {StaffId}", staffId);

                return new RegistrationResult
                {
                    Success = true,
                    Message = $"{request.Position} registered successfully",
                    GeneratedId = staffId,
                    LoginCredentials = new LoginCredentials
                    {
                        PhoneNumber = request.PhoneNumber,
                        Password = password
                    }
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Staff registration error");
                return new RegistrationResult
                {
                    Success = false,
                    Message = $"Registration failed: {ex.Message}"
                };
            }
        }

        #endregion

        #region Validation Methods

        /// <summary>
        /// Validate student registration request
        /// </summary>
        private UserValidationResult ValidateStudentRequest(StudentRegistrationRequest request)
        {
            if (request == null)
                return new UserValidationResult { IsValid = false, ErrorMessage = "Request cannot be null" };

            if (string.IsNullOrWhiteSpace(request.FullName))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Full name is required" };

            if (string.IsNullOrWhiteSpace(request.AdmissionNo))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Admission number is required" };

            if (string.IsNullOrWhiteSpace(request.Form))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Form is required" };

            if (string.IsNullOrWhiteSpace(request.Class))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Class is required" };

            if (string.IsNullOrWhiteSpace(request.Gender))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Gender is required" };

            if (string.IsNullOrWhiteSpace(request.ParentPhone))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Parent phone number is required" };

            if (request.DateOfBirth == default(DateTime) || request.DateOfBirth > DateTime.Now.AddYears(-10))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Valid date of birth is required" };

            if (!IsValidPhoneNumber(request.ParentPhone))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Valid phone number is required" };

            return new UserValidationResult { IsValid = true };
        }

        /// <summary>
        /// Validate teacher registration request
        /// </summary>
        private UserValidationResult ValidateTeacherRequest(TeacherRegistrationRequest request)
        {
            if (request == null)
                return new UserValidationResult { IsValid = false, ErrorMessage = "Request cannot be null" };

            if (string.IsNullOrWhiteSpace(request.FullName))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Full name is required" };

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Phone number is required" };

            if (string.IsNullOrWhiteSpace(request.EmployeeType))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Employee type is required" };

            if (!new[] { "BOM", "NTSC" }.Contains(request.EmployeeType))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Employee type must be BOM or NTSC" };

            if (request.EmployeeType == "NTSC" && request.NtscPayment <= 0)
                return new UserValidationResult { IsValid = false, ErrorMessage = "NTSC payment amount is required for NTSC teachers" };

            if (!IsValidPhoneNumber(request.PhoneNumber))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Valid phone number is required" };

            return new UserValidationResult { IsValid = true };
        }

        /// <summary>
        /// Validate staff registration request
        /// </summary>
        private UserValidationResult ValidateStaffRequest(StaffRegistrationRequest request)
        {
            if (request == null)
                return new UserValidationResult { IsValid = false, ErrorMessage = "Request cannot be null" };

            if (string.IsNullOrWhiteSpace(request.FullName))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Full name is required" };

            if (string.IsNullOrWhiteSpace(request.PhoneNumber))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Phone number is required" };

            if (string.IsNullOrWhiteSpace(request.Position))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Position is required" };

            var validPositions = new[] { "Principal", "DeputyPrincipal", "Secretary", "Bursar" };
            if (!validPositions.Contains(request.Position))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Invalid position" };

            if (!IsValidPhoneNumber(request.PhoneNumber))
                return new UserValidationResult { IsValid = false, ErrorMessage = "Valid phone number is required" };

            return new UserValidationResult { IsValid = true };
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if current user is authorized secretary
        /// </summary>
        private async Task<bool> IsSecretaryAuthorizedAsync()
        {
            try
            {
                var userType = await SecureStorage.GetAsync("user_type");
                return userType == "Secretary" || userType == "Principal";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get current user ID from auth service
        /// </summary>
        private async Task<string> GetCurrentUserIdAsync()
        {
            try
            {
                var userId = await SecureStorage.GetAsync("user_id");
                return userId ?? "system";
            }
            catch
            {
                return "system";
            }
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
        /// Validate phone number format
        /// </summary>
        private bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // Remove all non-digit characters except +
            var cleaned = new string(phoneNumber.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // Check various Kenyan phone number formats
            return cleaned.Length >= 10 &&
                   (cleaned.StartsWith("+254") ||
                    cleaned.StartsWith("254") ||
                    cleaned.StartsWith("0") ||
                    cleaned.Length == 9);
        }

        /// <summary>
        /// Create default fees record for new student
        /// </summary>
        private async Task CreateDefaultFeesRecordAsync(string studentId)
        {
            try
            {
                var fees = new FeesEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = studentId,
                    TotalFees = 80000m, // Default fees amount
                    PaidAmount = 0m,
                    Balance = 80000m,
                    Year = DateTime.Now.Year,
                    Term = GetCurrentTerm(),
                    DueDate = DateTime.Now.AddDays(30),
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _supabaseClient.From<FeesEntity>().Insert(fees);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating default fees record");
                // Don't throw - this is not critical for student registration
            }
        }

        /// <summary>
        /// Get all registered students (for secretary view)
        /// </summary>
        public async Task<List<StudentSummary>> GetAllStudentsAsync()
        {
            try
            {
                if (!await IsSecretaryAuthorizedAsync())
                {
                    throw new UnauthorizedAccessException("Only secretary can view all students");
                }

                var students = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.IsActive)
                    .Order(x => x.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                var studentList = new List<StudentSummary>();

                if (students?.Models != null)
                {
                    foreach (var student in students.Models)
                    {
                        studentList.Add(new StudentSummary
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

                return studentList;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all students");
                throw;
            }
        }

        /// <summary>
        /// Update student information
        /// </summary>
        public async Task<bool> UpdateStudentAsync(string studentId, StudentUpdateRequest request)
        {
            try
            {
                if (!await IsSecretaryAuthorizedAsync())
                {
                    return false;
                }

                var student = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.StudentId == studentId)
                    .Single();

                if (student == null)
                {
                    return false;
                }

                // Update student information
                student.FullName = request.FullName ?? student.FullName;
                student.Form = request.Form ?? student.Form;
                student.Class = request.Class ?? student.Class;
                student.Email = request.Email ?? student.Email;
                student.Address = request.Address ?? student.Address;

                await student.Update<StudentEntity>();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating student");
                return false;
            }
        }

        #endregion
    }

    #region Request Models

    public class StudentRegistrationRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string ParentPhone { get; set; } = string.Empty;
    }

    public class TeacherRegistrationRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string EmployeeType { get; set; } = string.Empty; // BOM, NTSC
        public string? TscNumber { get; set; }
        public decimal? NtscPayment { get; set; }
        public List<string>? Subjects { get; set; }
        public List<string>? AssignedForms { get; set; }
        public string? Qualification { get; set; }
        public string? Email { get; set; }
        public string Department { get; set; } = string.Empty; // Optional, e.g. Science, Arts
    }

    public class StaffRegistrationRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty; // Principal, DeputyPrincipal, Secretary, Bursar
        public string? Department { get; set; }
    }

    public class StudentUpdateRequest
    {
        public string? FullName { get; set; }
        public string? Form { get; set; }
        public string? Class { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }

    #endregion

    #region Response Models

    public class RegistrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? GeneratedId { get; set; }
        public LoginCredentials? LoginCredentials { get; set; }
    }

    public class LoginCredentials
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class StudentSummary
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    #endregion
}