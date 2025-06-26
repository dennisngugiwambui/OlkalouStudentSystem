// ===============================
// Models/Data/SupabaseModels.cs - Enhanced Entity Models for Supabase
// ===============================
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OlkalouStudentSystem.Models.Data
{
    #region Base Models

    /// <summary>
    /// Base model for all Supabase entities with common audit fields
    /// </summary>
    public abstract class BaseSupabaseModel : BaseModel
    {
        [Column("created_at")]
        public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public virtual DateTime? UpdatedAt { get; set; }

        [Column("created_by")]
        public virtual string? CreatedBy { get; set; }

        [Column("updated_by")]
        public virtual string? UpdatedBy { get; set; }

        /// <summary>
        /// Update the UpdatedAt timestamp
        /// </summary>
        public virtual void Touch()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Validate the model
        /// </summary>
        public virtual ValidationResult Validate()
        {
            var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            var validationContext = new ValidationContext(this);

            Validator.TryValidateObject(this, validationContext, validationResults, true);

            return new ValidationResult
            {
                IsValid = !validationResults.Any(),
                Errors = validationResults.Select(vr => vr.ErrorMessage ?? "Unknown error").ToList()
            };
        }
    }

    /// <summary>
    /// Validation result container
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public string ErrorMessage => string.Join("; ", Errors);
    }

    #endregion

    #region Authentication Models

    /// <summary>
    /// User Entity for Authentication
    /// </summary>
    [Table("users")]
    public class UserEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        [Required]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("phone_number")]
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 15 characters")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Column("password")]
        [Required(ErrorMessage = "Password is required")]
        [StringLength(255, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Column("user_type")]
        [Required(ErrorMessage = "User type is required")]
        [StringLength(50)]
        public string UserType { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("last_login")]
        public DateTime? LastLogin { get; set; }

        [Column("password_changed")]
        public bool PasswordChanged { get; set; } = false;

        [Column("password_changed_at")]
        public DateTime? PasswordChangedAt { get; set; }

        [Column("failed_login_attempts")]
        public int FailedLoginAttempts { get; set; } = 0;

        [Column("locked_until")]
        public DateTime? LockedUntil { get; set; }

        [Column("email")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255)]
        public string? Email { get; set; }

        [Column("profile_completed")]
        public bool ProfileCompleted { get; set; } = false;

        [Column("terms_accepted")]
        public bool TermsAccepted { get; set; } = false;

        [Column("terms_accepted_at")]
        public DateTime? TermsAcceptedAt { get; set; }

        /// <summary>
        /// Check if account is locked
        /// </summary>
        public bool IsLocked => LockedUntil.HasValue && LockedUntil.Value > DateTime.UtcNow;

        /// <summary>
        /// Check if password needs to be changed
        /// </summary>
        public bool RequiresPasswordChange => !PasswordChanged ||
            (PasswordChangedAt.HasValue && PasswordChangedAt.Value.AddDays(90) < DateTime.UtcNow);

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Additional custom validations
            if (!Enum.TryParse<UserType>(UserType, out _))
            {
                result.Errors.Add("Invalid user type");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// User types enumeration
    /// </summary>
    public enum UserType
    {
        Student,
        Teacher,
        Principal,
        DeputyPrincipal,
        Secretary,
        Bursar,
        Librarian,
        Staff
    }

    #endregion

    #region Student Models

    /// <summary>
    /// Student Entity
    /// </summary>
    [Table("students")]
    public class StudentEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("user_id")]
        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;

        [Column("student_id")]
        [Required(ErrorMessage = "Student ID is required")]
        [StringLength(50)]
        public string StudentId { get; set; } = string.Empty;

        [Column("admission_no")]
        [Required(ErrorMessage = "Admission number is required")]
        [StringLength(50)]
        public string AdmissionNo { get; set; } = string.Empty;

        [Column("full_name")]
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(255, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 255 characters")]
        public string FullName { get; set; } = string.Empty;

        [Column("form")]
        [Required(ErrorMessage = "Form is required")]
        [StringLength(20)]
        public string Form { get; set; } = string.Empty;

        [Column("class")]
        [Required(ErrorMessage = "Class is required")]
        [StringLength(20)]
        public string Class { get; set; } = string.Empty;

        [Column("email")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(255)]
        public string? Email { get; set; }

        [Column("date_of_birth")]
        [Required(ErrorMessage = "Date of birth is required")]
        public DateTime DateOfBirth { get; set; }

        [Column("gender")]
        [Required(ErrorMessage = "Gender is required")]
        [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Gender must be Male, Female, or Other")]
        public string Gender { get; set; } = string.Empty;

        [Column("address")]
        [StringLength(500)]
        public string? Address { get; set; }

        [Column("parent_phone")]
        [Required(ErrorMessage = "Parent phone number is required")]
        [Phone(ErrorMessage = "Invalid parent phone number")]
        [StringLength(15, MinimumLength = 10)]
        public string ParentPhone { get; set; } = string.Empty;

        [Column("guardian_name")]
        [StringLength(255)]
        public string? GuardianName { get; set; }

        [Column("guardian_phone")]
        [Phone(ErrorMessage = "Invalid guardian phone number")]
        [StringLength(15)]
        public string? GuardianPhone { get; set; }

        [Column("emergency_contact")]
        [Phone(ErrorMessage = "Invalid emergency contact number")]
        [StringLength(15)]
        public string? EmergencyContact { get; set; }

        [Column("year")]
        [Range(2020, 2050, ErrorMessage = "Year must be between 2020 and 2050")]
        public int Year { get; set; } = DateTime.UtcNow.Year;

        [Column("term")]
        [Range(1, 3, ErrorMessage = "Term must be between 1 and 3")]
        public int Term { get; set; } = 1;

        [Column("enrollment_date")]
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("profile_picture_url")]
        [Url(ErrorMessage = "Invalid profile picture URL")]
        public string? ProfilePictureUrl { get; set; }

        [Column("status")]
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";

        [Column("medical_info")]
        [StringLength(1000)]
        public string? MedicalInfo { get; set; }

        [Column("nationality")]
        [StringLength(100)]
        public string Nationality { get; set; } = "Kenyan";

        [Column("religion")]
        [StringLength(100)]
        public string? Religion { get; set; }

        [Column("previous_school")]
        [StringLength(255)]
        public string? PreviousSchool { get; set; }

        [Column("kcse_index_number")]
        [StringLength(50)]
        public string? KcseIndexNumber { get; set; }

        /// <summary>
        /// Calculate age from date of birth
        /// </summary>
        public int Age
        {
            get
            {
                var today = DateTime.Today;
                var age = today.Year - DateOfBirth.Year;
                if (DateOfBirth.Date > today.AddYears(-age)) age--;
                return age;
            }
        }

        /// <summary>
        /// Get full class name
        /// </summary>
        public string FullClassName => $"{Form}{Class}";

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate age constraints
            if (Age < 10 || Age > 25)
            {
                result.Errors.Add("Student age must be between 10 and 25 years");
                result.IsValid = false;
            }

            // Validate form and class combination
            if (!IsValidFormClassCombination(Form, Class))
            {
                result.Errors.Add("Invalid form and class combination");
                result.IsValid = false;
            }

            return result;
        }

        private static bool IsValidFormClassCombination(string form, string classCode)
        {
            var validForms = new[] { "Form1", "Form2", "Form3", "Form4" };
            var validClasses = new[] { "A", "B", "C", "S", "N" };

            return validForms.Contains(form) && validClasses.Contains(classCode);
        }
    }

    #endregion

    #region Teacher Models

    /// <summary>
    /// Teacher Entity
    /// </summary>
    [Table("teachers")]
    public class TeacherEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("user_id")]
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Column("teacher_id")]
        [Required]
        [StringLength(50)]
        public string TeacherId { get; set; } = string.Empty;

        [Column("full_name")]
        [Required]
        [StringLength(255, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Column("employee_type")]
        [Required]
        [StringLength(50)]
        public string EmployeeType { get; set; } = string.Empty;

        [Column("tsc_number")]
        [StringLength(50)]
        public string? TscNumber { get; set; }

        [Column("ntsc_payment")]
        [Range(0, double.MaxValue, ErrorMessage = "NTSC payment must be positive")]
        public decimal? NtscPayment { get; set; }

        [Column("subjects")]
        [JsonConverter(typeof(JsonStringArrayConverter))]
        public List<string> Subjects { get; set; } = new();

        [Column("assigned_forms")]
        [JsonConverter(typeof(JsonStringArrayConverter))]
        public List<string> AssignedForms { get; set; } = new();

        [Column("qualification")]
        [StringLength(500)]
        public string? Qualification { get; set; }

        [Column("experience_years")]
        [Range(0, 50, ErrorMessage = "Experience years must be between 0 and 50")]
        public int? ExperienceYears { get; set; }

        [Column("employment_date")]
        public DateTime? EmploymentDate { get; set; }

        [Column("department")]
        [StringLength(100)]
        public string? Department { get; set; }

        [Column("is_class_teacher")]
        public bool IsClassTeacher { get; set; } = false;

        [Column("class_teacher_for")]
        [StringLength(50)]
        public string? ClassTeacherFor { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("profile_picture_url")]
        [Url]
        public string? ProfilePictureUrl { get; set; }

        [Column("performance_rating")]
        [Range(1, 5, ErrorMessage = "Performance rating must be between 1 and 5")]
        public decimal? PerformanceRating { get; set; }

        [Column("email")]
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [Column("phone_number")]
        [Phone]
        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [Column("national_id")]
        [StringLength(20)]
        public string? NationalId { get; set; }

        [Column("bank_account")]
        [StringLength(50)]
        public string? BankAccount { get; set; }

        [Column("monthly_salary")]
        [Range(0, double.MaxValue)]
        public decimal? MonthlySalary { get; set; }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate employee type
            var validEmployeeTypes = new[] { "BOM", "NTSC", "Contract", "Volunteer" };
            if (!validEmployeeTypes.Contains(EmployeeType))
            {
                result.Errors.Add("Invalid employee type");
                result.IsValid = false;
            }

            // TSC number required for NTSC employees
            if (EmployeeType == "NTSC" && string.IsNullOrWhiteSpace(TscNumber))
            {
                result.Errors.Add("TSC number is required for NTSC employees");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Staff Models

    /// <summary>
    /// Staff Entity
    /// </summary>
    [Table("staff")]
    public class StaffEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("user_id")]
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Column("staff_id")]
        [Required]
        [StringLength(50)]
        public string StaffId { get; set; } = string.Empty;

        [Column("full_name")]
        [Required]
        [StringLength(255, MinimumLength = 2)]
        public string FullName { get; set; } = string.Empty;

        [Column("position")]
        [Required]
        [StringLength(100)]
        public string Position { get; set; } = string.Empty;

        [Column("department")]
        [StringLength(100)]
        public string? Department { get; set; }

        [Column("employment_date")]
        public DateTime? EmploymentDate { get; set; }

        [Column("qualification")]
        [StringLength(500)]
        public string? Qualification { get; set; }

        [Column("salary")]
        [Range(0, double.MaxValue)]
        public decimal? Salary { get; set; }

        [Column("office_location")]
        [StringLength(100)]
        public string? OfficeLocation { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("profile_picture_url")]
        [Url]
        public string? ProfilePictureUrl { get; set; }

        [Column("permissions")]
        [JsonConverter(typeof(JsonStringArrayConverter))]
        public List<string> Permissions { get; set; } = new();

        [Column("email")]
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }

        [Column("phone_number")]
        [Phone]
        [StringLength(15)]
        public string? PhoneNumber { get; set; }

        [Column("national_id")]
        [StringLength(20)]
        public string? NationalId { get; set; }

        [Column("bank_account")]
        [StringLength(50)]
        public string? BankAccount { get; set; }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate position
            var validPositions = new[] { "Principal", "DeputyPrincipal", "Secretary", "Bursar", "Librarian", "LabTechnician", "ComputerLabTechnician", "Cook", "Gardener", "BoardingMaster" };
            if (!validPositions.Contains(Position))
            {
                result.Errors.Add("Invalid staff position");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Fees Models

    /// <summary>
    /// Fees Entity
    /// </summary>
    [Table("fees")]
    public class FeesEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("student_id")]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Column("total_fees")]
        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Total fees must be positive")]
        public decimal TotalFees { get; set; }

        [Column("paid_amount")]
        [Range(0, double.MaxValue, ErrorMessage = "Paid amount must be positive")]
        public decimal PaidAmount { get; set; }

        [Column("balance")]
        [Range(double.MinValue, double.MaxValue)]
        public decimal Balance { get; set; }

        [Column("year")]
        [Required]
        [Range(2020, 2050)]
        public int Year { get; set; } = DateTime.UtcNow.Year;

        [Column("term")]
        [Required]
        [Range(1, 3)]
        public int Term { get; set; } = 1;

        [Column("due_date")]
        [Required]
        public DateTime DueDate { get; set; }

        [Column("payment_status")]
        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        [Column("last_payment_date")]
        public DateTime? LastPaymentDate { get; set; }

        [Column("discount_amount")]
        [Range(0, double.MaxValue)]
        public decimal? DiscountAmount { get; set; }

        [Column("discount_reason")]
        [StringLength(500)]
        public string? DiscountReason { get; set; }

        /// <summary>
        /// Check if fees are overdue
        /// </summary>
        public bool IsOverdue => DateTime.UtcNow > DueDate && Balance > 0;

        /// <summary>
        /// Get days overdue
        /// </summary>
        public int DaysOverdue => IsOverdue ? (DateTime.UtcNow - DueDate).Days : 0;

        /// <summary>
        /// Calculate balance
        /// </summary>
        public void CalculateBalance()
        {
            Balance = TotalFees - PaidAmount - (DiscountAmount ?? 0);
        }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate payment status
            var validStatuses = new[] { "Pending", "Partial", "Paid", "Overdue" };
            if (!validStatuses.Contains(PaymentStatus))
            {
                result.Errors.Add("Invalid payment status");
                result.IsValid = false;
            }

            // Validate paid amount doesn't exceed total fees
            if (PaidAmount > TotalFees)
            {
                result.Errors.Add("Paid amount cannot exceed total fees");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Fees Payment Entity
    /// </summary>
    [Table("fees_payments")]
    public class FeesPaymentEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("fees_id")]
        public string? FeesId { get; set; }

        [Column("student_id")]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Column("amount")]
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [Column("payment_method")]
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Column("receipt_number")]
        [Required]
        [StringLength(100)]
        public string ReceiptNumber { get; set; } = string.Empty;

        [Column("transaction_id")]
        [StringLength(100)]
        public string? TransactionId { get; set; }

        [Column("slip_image_url")]
        [Url]
        public string? SlipImageUrl { get; set; }

        [Column("is_approved")]
        public bool IsApproved { get; set; } = false;

        [Column("is_scanned")]
        public bool IsScanned { get; set; } = false;

        [Column("payment_date")]
        [Required]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Column("verified_by")]
        [StringLength(100)]
        public string? VerifiedBy { get; set; }

        [Column("verification_date")]
        public DateTime? VerificationDate { get; set; }

        [Column("description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Column("received_by")]
        [Required]
        [StringLength(100)]
        public string ReceivedBy { get; set; } = string.Empty;

        [Column("bank_name")]
        [StringLength(100)]
        public string? BankName { get; set; }

        [Column("account_number")]
        [StringLength(50)]
        public string? AccountNumber { get; set; }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate payment method
            var validMethods = new[] { "Cash", "M-Pesa", "Bank Transfer", "Cheque", "Card" };
            if (!validMethods.Contains(PaymentMethod))
            {
                result.Errors.Add("Invalid payment method");
                result.IsValid = false;
            }

            // Bank details required for bank transfers
            if (PaymentMethod == "Bank Transfer" &&
                (string.IsNullOrWhiteSpace(BankName) || string.IsNullOrWhiteSpace(AccountNumber)))
            {
                result.Errors.Add("Bank name and account number are required for bank transfers");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Academic Models

    /// <summary>
    /// Assignment Entity
    /// </summary>
    [Table("assignments")]
    public class AssignmentEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("assignment_id")]
        [Required]
        [StringLength(50)]
        public string AssignmentId { get; set; } = string.Empty;

        [Column("title")]
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Column("subject")]
        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Column("description")]
        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Column("instructions")]
        [StringLength(2000)]
        public string? Instructions { get; set; }

        [Column("due_date")]
        [Required]
        public DateTime DueDate { get; set; }

        [Column("max_marks")]
        [Required]
        [Range(1, 1000, ErrorMessage = "Max marks must be between 1 and 1000")]
        public int MaxMarks { get; set; }

        [Column("form")]
        [Required]
        [StringLength(20)]
        public string Form { get; set; } = string.Empty;

        [Column("target_classes")]
        [JsonConverter(typeof(JsonStringArrayConverter))]
        public List<string> TargetClasses { get; set; } = new();

        [Column("file_path")]
        [StringLength(500)]
        public string? FilePath { get; set; }

        [Column("file_url")]
        [Url]
        public string? FileUrl { get; set; }

        [Column("assignment_type")]
        [Required]
        [StringLength(50)]
        public string AssignmentType { get; set; } = "Assignment";

        [Column("is_published")]
        public bool IsPublished { get; set; } = true;

        [Column("allow_late_submission")]
        public bool AllowLateSubmission { get; set; } = false;

        [Column("late_penalty_percentage")]
        [Range(0, 100)]
        public decimal? LatePenaltyPercentage { get; set; }

        /// <summary>
        /// Check if assignment is overdue
        /// </summary>
        public bool IsOverdue => DateTime.UtcNow > DueDate;

        /// <summary>
        /// Get days until due
        /// </summary>
        public int DaysUntilDue => (DueDate - DateTime.UtcNow).Days;

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate assignment type
            var validTypes = new[] { "Assignment", "Project", "Test", "Quiz", "Lab Report" };
            if (!validTypes.Contains(AssignmentType))
            {
                result.Errors.Add("Invalid assignment type");
                result.IsValid = false;
            }

            // Due date should be in the future
            if (DueDate <= DateTime.UtcNow)
            {
                result.Errors.Add("Due date must be in the future");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Assignment Submission Entity
    /// </summary>
    [Table("assignment_submissions")]
    public class AssignmentSubmissionEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("assignment_id")]
        [Required]
        public string AssignmentId { get; set; } = string.Empty;

        [Column("student_id")]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Column("submission_text")]
        [StringLength(5000)]
        public string? SubmissionText { get; set; }

        [Column("submission_path")]
        [StringLength(500)]
        public string? SubmissionPath { get; set; }

        [Column("submission_url")]
        [Url]
        public string? SubmissionUrl { get; set; }

        [Column("obtained_marks")]
        [Range(0, 1000)]
        public int? ObtainedMarks { get; set; }

        [Column("teacher_comments")]
        [StringLength(1000)]
        public string? TeacherComments { get; set; }

        [Column("status")]
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Submitted";

        [Column("submission_date")]
        [Required]
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;

        [Column("graded_by")]
        [StringLength(100)]
        public string? GradedBy { get; set; }

        [Column("graded_date")]
        public DateTime? GradedDate { get; set; }

        [Column("is_late")]
        public bool IsLate { get; set; } = false;

        [Column("late_penalty_applied")]
        [Range(0, 100)]
        public decimal? LatePenaltyApplied { get; set; }

        [Column("final_marks")]
        [Range(0, 1000)]
        public int? FinalMarks { get; set; }

        /// <summary>
        /// Calculate percentage
        /// </summary>
        public decimal? Percentage => ObtainedMarks.HasValue && FinalMarks.HasValue && FinalMarks > 0
            ? (decimal)ObtainedMarks.Value / FinalMarks.Value * 100
            : null;

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate status
            var validStatuses = new[] { "Submitted", "Graded", "Late", "Pending Review", "Returned" };
            if (!validStatuses.Contains(Status))
            {
                result.Errors.Add("Invalid submission status");
                result.IsValid = false;
            }

            // Must have either text or file submission
            if (string.IsNullOrWhiteSpace(SubmissionText) &&
                string.IsNullOrWhiteSpace(SubmissionPath) &&
                string.IsNullOrWhiteSpace(SubmissionUrl))
            {
                result.Errors.Add("Submission must include text, file, or URL");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Mark Entity
    /// </summary>
    [Table("marks")]
    public class MarkEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("student_id")]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Column("subject")]
        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Column("opening_marks")]
        [Range(0, 100)]
        public decimal? OpeningMarks { get; set; }

        [Column("midterm_marks")]
        [Range(0, 100)]
        public decimal? MidtermMarks { get; set; }

        [Column("final_exam_marks")]
        [Range(0, 100)]
        public decimal? FinalExamMarks { get; set; }

        [Column("total_marks")]
        [Required]
        [Range(0, 100)]
        public decimal TotalMarks { get; set; }

        [Column("max_marks")]
        [Range(1, 100)]
        public decimal MaxMarks { get; set; } = 100;

        [Column("percentage")]
        [Range(0, 100)]
        public decimal Percentage { get; set; }

        [Column("grade")]
        [Required]
        [StringLength(5)]
        public string Grade { get; set; } = string.Empty;

        [Column("points")]
        [Range(0, 12)]
        public decimal? Points { get; set; }

        [Column("term")]
        [Required]
        [Range(1, 3)]
        public int Term { get; set; }

        [Column("year")]
        [Required]
        [Range(2020, 2050)]
        public int Year { get; set; } = DateTime.UtcNow.Year;

        [Column("exam_type")]
        [Required]
        [StringLength(50)]
        public string ExamType { get; set; } = "End of Term";

        [Column("teacher_id")]
        [Required]
        public string TeacherId { get; set; } = string.Empty;

        [Column("is_approved")]
        public bool IsApproved { get; set; } = false;

        [Column("approved_by")]
        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        [Column("approval_date")]
        public DateTime? ApprovalDate { get; set; }

        [Column("teacher_comments")]
        [StringLength(500)]
        public string? TeacherComments { get; set; }

        /// <summary>
        /// Calculate total marks based on components
        /// </summary>
        public void CalculateTotalMarks()
        {
            var opening = OpeningMarks ?? 0;
            var midterm = MidtermMarks ?? 0;
            var final = FinalExamMarks ?? 0;

            // Standard Kenyan grading: Opening 15%, Midterm 15%, Final 70%
            TotalMarks = (opening * 0.15m) + (midterm * 0.15m) + (final * 0.70m);
            Percentage = (TotalMarks / MaxMarks) * 100;
        }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate exam type
            var validExamTypes = new[] { "CAT", "Mid-Term", "End of Term", "Mock", "KCSE" };
            if (!validExamTypes.Contains(ExamType))
            {
                result.Errors.Add("Invalid exam type");
                result.IsValid = false;
            }

            // Validate grade
            var validGrades = new[] { "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D+", "D", "D-", "E" };
            if (!validGrades.Contains(Grade))
            {
                result.Errors.Add("Invalid grade");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Grading System Entity
    /// </summary>
    [Table("grading_system")]
    public class GradingSystemEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("grade")]
        [Required]
        [StringLength(5)]
        public string Grade { get; set; } = string.Empty;

        [Column("min_percentage")]
        [Required]
        [Range(0, 100)]
        public decimal MinPercentage { get; set; }

        [Column("max_percentage")]
        [Required]
        [Range(0, 100)]
        public decimal MaxPercentage { get; set; }

        [Column("points")]
        [Required]
        [Range(0, 12)]
        public decimal Points { get; set; }

        [Column("description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Min percentage should be less than max percentage
            if (MinPercentage >= MaxPercentage)
            {
                result.Errors.Add("Minimum percentage must be less than maximum percentage");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Library Models

    /// <summary>
    /// Library Book Entity
    /// </summary>
    [Table("library_books")]
    public class LibraryBookEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("book_id")]
        [Required]
        [StringLength(50)]
        public string BookId { get; set; } = string.Empty;

        [Column("title")]
        [Required]
        [StringLength(255, MinimumLength = 2)]
        public string Title { get; set; } = string.Empty;

        [Column("author")]
        [Required]
        [StringLength(255, MinimumLength = 2)]
        public string Author { get; set; } = string.Empty;

        [Column("isbn")]
        [StringLength(20)]
        public string? ISBN { get; set; }

        [Column("category")]
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Column("subcategory")]
        [StringLength(100)]
        public string? Subcategory { get; set; }

        [Column("publisher")]
        [StringLength(255)]
        public string? Publisher { get; set; }

        [Column("publication_year")]
        [Range(1900, 2050)]
        public int? PublicationYear { get; set; }

        [Column("edition")]
        [StringLength(50)]
        public string? Edition { get; set; }

        [Column("language")]
        [StringLength(50)]
        public string Language { get; set; } = "English";

        [Column("total_copies")]
        [Required]
        [Range(1, 1000)]
        public int TotalCopies { get; set; }

        [Column("available_copies")]
        [Range(0, 1000)]
        public int AvailableCopies { get; set; }

        [Column("damaged_copies")]
        [Range(0, 1000)]
        public int DamagedCopies { get; set; } = 0;

        [Column("lost_copies")]
        [Range(0, 1000)]
        public int LostCopies { get; set; } = 0;

        [Column("description")]
        [StringLength(1000)]
        public string? Description { get; set; }

        [Column("image_url")]
        [Url]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Check if book is available
        /// </summary>
        public bool IsAvailable => AvailableCopies > 0;

        /// <summary>
        /// Get total issued copies
        /// </summary>
        public int IssuedCopies => TotalCopies - AvailableCopies - DamagedCopies - LostCopies;

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Available + Damaged + Lost should not exceed Total
            if (AvailableCopies + DamagedCopies + LostCopies > TotalCopies)
            {
                result.Errors.Add("Sum of available, damaged, and lost copies cannot exceed total copies");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Book Issue Entity
    /// </summary>
    [Table("book_issues")]
    public class BookIssueEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("book_id")]
        [Required]
        public string BookId { get; set; } = string.Empty;

        [Column("student_id")]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Column("issue_date")]
        [Required]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        [Column("due_date")]
        [Required]
        public DateTime DueDate { get; set; }

        [Column("return_date")]
        public DateTime? ReturnDate { get; set; }

        [Column("status")]
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Issued";

        [Column("issued_by")]
        [Required]
        [StringLength(100)]
        public string IssuedBy { get; set; } = string.Empty;

        [Column("returned_to")]
        [StringLength(100)]
        public string? ReturnedTo { get; set; }

        [Column("fine_amount")]
        [Range(0, double.MaxValue)]
        public decimal? FineAmount { get; set; }

        [Column("fine_paid")]
        public bool FinePaid { get; set; } = false;

        [Column("notes")]
        [StringLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// Check if book is overdue
        /// </summary>
        public bool IsOverdue => Status == "Issued" && DateTime.UtcNow > DueDate;

        /// <summary>
        /// Get days overdue
        /// </summary>
        public int DaysOverdue => IsOverdue ? (DateTime.UtcNow - DueDate).Days : 0;

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate status
            var validStatuses = new[] { "Issued", "Returned", "Overdue", "Lost", "Damaged" };
            if (!validStatuses.Contains(Status))
            {
                result.Errors.Add("Invalid issue status");
                result.IsValid = false;
            }

            // Due date should be after issue date
            if (DueDate <= IssueDate)
            {
                result.Errors.Add("Due date must be after issue date");
                result.IsValid = false;
            }

            // Return date should be after issue date
            if (ReturnDate.HasValue && ReturnDate.Value < IssueDate)
            {
                result.Errors.Add("Return date cannot be before issue date");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Activity Models

    /// <summary>
    /// Activity Entity
    /// </summary>
    [Table("activities")]
    public class ActivityEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("activity_id")]
        [Required]
        [StringLength(50)]
        public string ActivityId { get; set; } = string.Empty;

        [Column("title")]
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Column("date")]
        [Required]
        public DateTime Date { get; set; }

        [Column("start_time")]
        [Required]
        public DateTime StartTime { get; set; }

        [Column("end_time")]
        [Required]
        public DateTime EndTime { get; set; }

        [Column("venue")]
        [StringLength(255)]
        public string? Venue { get; set; }

        [Column("activity_type")]
        [Required]
        [StringLength(50)]
        public string ActivityType { get; set; } = string.Empty;

        [Column("organizer")]
        [Required]
        [StringLength(100)]
        public string Organizer { get; set; } = string.Empty;

        [Column("target_forms")]
        [JsonConverter(typeof(JsonStringArrayConverter))]
        public List<string> TargetForms { get; set; } = new();

        [Column("is_optional")]
        public bool IsOptional { get; set; } = true;

        [Column("registration_deadline")]
        public DateTime? RegistrationDeadline { get; set; }

        [Column("max_participants")]
        [Range(1, 10000)]
        public int? MaxParticipants { get; set; }

        [Column("current_participants")]
        [Range(0, 10000)]
        public int CurrentParticipants { get; set; } = 0;

        [Column("requirements")]
        [StringLength(1000)]
        public string? Requirements { get; set; }

        [Column("status")]
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Check if activity is upcoming
        /// </summary>
        public bool IsUpcoming => Date > DateTime.UtcNow && Status == "Active";

        /// <summary>
        /// Check if activity is past
        /// </summary>
        public bool IsPast => Date < DateTime.UtcNow;

        /// <summary>
        /// Check if registration is open
        /// </summary>
        public bool CanRegister => IsOptional &&
                                  RegistrationDeadline.HasValue &&
                                  RegistrationDeadline > DateTime.UtcNow &&
                                  (!MaxParticipants.HasValue || CurrentParticipants < MaxParticipants.Value) &&
                                  Status == "Active";

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // End time should be after start time
            if (EndTime <= StartTime)
            {
                result.Errors.Add("End time must be after start time");
                result.IsValid = false;
            }

            // Registration deadline should be before activity date
            if (RegistrationDeadline.HasValue && RegistrationDeadline >= Date)
            {
                result.Errors.Add("Registration deadline must be before activity date");
                result.IsValid = false;
            }

            // Validate activity type
            var validActivityTypes = new[] { "Sports", "Academic", "Cultural", "Social", "Religious", "Field Trip" };
            if (!validActivityTypes.Contains(ActivityType))
            {
                result.Errors.Add("Invalid activity type");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Activity Registration Entity
    /// </summary>
    [Table("activity_registrations")]
    public class ActivityRegistrationEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("activity_id")]
        [Required]
        public string ActivityId { get; set; } = string.Empty;

        [Column("student_id")]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Column("registration_date")]
        [Required]
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        [Column("status")]
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Registered";

        [Column("payment_status")]
        [StringLength(50)]
        public string? PaymentStatus { get; set; }

        [Column("attendance_marked")]
        public bool AttendanceMarked { get; set; } = false;

        [Column("attendance_date")]
        public DateTime? AttendanceDate { get; set; }

        [Column("notes")]
        [StringLength(500)]
        public string? Notes { get; set; }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate status
            var validStatuses = new[] { "Registered", "Attended", "Absent", "Cancelled" };
            if (!validStatuses.Contains(Status))
            {
                result.Errors.Add("Invalid registration status");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Achievement Models

    /// <summary>
    /// Achievement Entity
    /// </summary>
    [Table("achievements")]
    public class AchievementEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("student_id")]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Column("achievement_id")]
        [Required]
        [StringLength(50)]
        public string AchievementId { get; set; } = string.Empty;

        [Column("title")]
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        [Required]
        [StringLength(1000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Column("type")]
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [Column("date")]
        [Required]
        public DateTime Date { get; set; }

        [Column("awarded_by")]
        [Required]
        [StringLength(100)]
        public string AwardedBy { get; set; } = string.Empty;

        [Column("category")]
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Column("level")]
        [Required]
        [StringLength(50)]
        public string Level { get; set; } = string.Empty;

        [Column("position")]
        [Range(1, 100)]
        public int? Position { get; set; }

        [Column("points")]
        [Range(0, 1000)]
        public int? Points { get; set; }

        [Column("certificate_path")]
        [StringLength(500)]
        public string? CertificatePath { get; set; }

        [Column("certificate_url")]
        [Url]
        public string? CertificateUrl { get; set; }

        [Column("is_verified")]
        public bool IsVerified { get; set; } = false;

        [Column("verified_by")]
        [StringLength(100)]
        public string? VerifiedBy { get; set; }

        [Column("verification_date")]
        public DateTime? VerificationDate { get; set; }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate achievement type
            var validTypes = new[] { "Academic", "Sports", "Leadership", "Arts", "Community Service", "Other" };
            if (!validTypes.Contains(Type))
            {
                result.Errors.Add("Invalid achievement type");
                result.IsValid = false;
            }

            // Validate level
            var validLevels = new[] { "School", "County", "Regional", "National", "International" };
            if (!validLevels.Contains(Level))
            {
                result.Errors.Add("Invalid achievement level");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Communication Models

    /// <summary>
    /// Announcement Entity
    /// </summary>
    [Table("announcements")]
    public class AnnouncementEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("title")]
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Column("content")]
        [Required]
        [StringLength(5000, MinimumLength = 10)]
        public string Content { get; set; } = string.Empty;

        [Column("announcement_type")]
        [Required]
        [StringLength(50)]
        public string AnnouncementType { get; set; } = "General";

        [Column("priority")]
        [Required]
        [StringLength(20)]
        public string Priority { get; set; } = "Normal";

        [Column("target_audience")]
        [JsonConverter(typeof(JsonStringArrayConverter))]
        public List<string> TargetAudience { get; set; } = new();

        [Column("target_forms")]
        [JsonConverter(typeof(JsonStringArrayConverter))]
        public List<string> TargetForms { get; set; } = new();

        [Column("is_published")]
        public bool IsPublished { get; set; } = false;

        [Column("publish_date")]
        public DateTime? PublishDate { get; set; }

        [Column("expiry_date")]
        public DateTime? ExpiryDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Check if announcement is active
        /// </summary>
        public bool IsCurrentlyActive => IsPublished && IsActive &&
                               (PublishDate == null || PublishDate <= DateTime.UtcNow) &&
                               (ExpiryDate == null || ExpiryDate > DateTime.UtcNow);

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate announcement type
            var validTypes = new[] { "General", "Academic", "Event", "Emergency", "Fee", "Administrative" };
            if (!validTypes.Contains(AnnouncementType))
            {
                result.Errors.Add("Invalid announcement type");
                result.IsValid = false;
            }

            // Validate priority
            var validPriorities = new[] { "Low", "Normal", "High", "Urgent" };
            if (!validPriorities.Contains(Priority))
            {
                result.Errors.Add("Invalid priority level");
                result.IsValid = false;
            }

            // Expiry date should be after publish date
            if (ExpiryDate.HasValue && PublishDate.HasValue && ExpiryDate <= PublishDate)
            {
                result.Errors.Add("Expiry date must be after publish date");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Attendance Models

    /// <summary>
    /// Attendance Entity
    /// </summary>
    [Table("attendance")]
    public class AttendanceEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("student_id")]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Column("date")]
        [Required]
        public DateTime Date { get; set; }

        [Column("status")]
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        [Column("time_in")]
        public TimeSpan? TimeIn { get; set; }

        [Column("time_out")]
        public TimeSpan? TimeOut { get; set; }

        [Column("reason")]
        [StringLength(500)]
        public string? Reason { get; set; }

        [Column("marked_by")]
        [Required]
        [StringLength(100)]
        public string MarkedBy { get; set; } = string.Empty;

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate status
            var validStatuses = new[] { "Present", "Absent", "Late", "Excused", "Sick" };
            if (!validStatuses.Contains(Status))
            {
                result.Errors.Add("Invalid attendance status");
                result.IsValid = false;
            }

            // Time out should be after time in
            if (TimeIn.HasValue && TimeOut.HasValue && TimeOut <= TimeIn)
            {
                result.Errors.Add("Time out must be after time in");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Disciplinary Models

    /// <summary>
    /// Disciplinary Issue Entity
    /// </summary>
    [Table("disciplinary_issues")]
    public class DisciplinaryIssueEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("student_id")]
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Column("issue_type")]
        [Required]
        [StringLength(100)]
        public string IssueType { get; set; } = string.Empty;

        [Column("issue_description")]
        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string IssueDescription { get; set; } = string.Empty;

        [Column("action_taken")]
        [StringLength(1000)]
        public string? ActionTaken { get; set; }

        [Column("severity_level")]
        [Required]
        [StringLength(20)]
        public string SeverityLevel { get; set; } = "Minor";

        [Column("is_suspension")]
        public bool IsSuspension { get; set; } = false;

        [Column("suspension_duration")]
        [Range(1, 365)]
        public int? SuspensionDuration { get; set; }

        [Column("suspension_start_date")]
        public DateTime? SuspensionStartDate { get; set; }

        [Column("suspension_end_date")]
        public DateTime? SuspensionEndDate { get; set; }

        [Column("issued_by")]
        [Required]
        [StringLength(100)]
        public string IssuedBy { get; set; } = string.Empty;

        [Column("approved_by")]
        [StringLength(100)]
        public string? ApprovedBy { get; set; }

        [Column("status")]
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [Column("issue_date")]
        [Required]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        [Column("approval_date")]
        public DateTime? ApprovalDate { get; set; }

        [Column("resolution_date")]
        public DateTime? ResolutionDate { get; set; }

        [Column("parent_notified")]
        public bool ParentNotified { get; set; } = false;

        [Column("notification_date")]
        public DateTime? NotificationDate { get; set; }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate severity level
            var validSeverityLevels = new[] { "Minor", "Major", "Critical" };
            if (!validSeverityLevels.Contains(SeverityLevel))
            {
                result.Errors.Add("Invalid severity level");
                result.IsValid = false;
            }

            // Validate status
            var validStatuses = new[] { "Pending", "Approved", "Rejected", "Resolved" };
            if (!validStatuses.Contains(Status))
            {
                result.Errors.Add("Invalid status");
                result.IsValid = false;
            }

            // Suspension validation
            if (IsSuspension)
            {
                if (!SuspensionDuration.HasValue)
                {
                    result.Errors.Add("Suspension duration is required for suspensions");
                    result.IsValid = false;
                }

                if (!SuspensionStartDate.HasValue)
                {
                    result.Errors.Add("Suspension start date is required for suspensions");
                    result.IsValid = false;
                }
            }

            return result;
        }
    }

    #endregion

    #region Timetable Models

    /// <summary>
    /// Timetable Entity
    /// </summary>
    [Table("timetable")]
    public class TimetableEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("class")]
        [Required]
        [StringLength(20)]
        public string Class { get; set; } = string.Empty;

        [Column("day_of_week")]
        [Required]
        [Range(1, 7)]
        public int DayOfWeek { get; set; }

        [Column("period")]
        [Required]
        [Range(1, 10)]
        public int Period { get; set; }

        [Column("start_time")]
        [Required]
        public TimeSpan StartTime { get; set; }

        [Column("end_time")]
        [Required]
        public TimeSpan EndTime { get; set; }

        [Column("subject")]
        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Column("teacher_id")]
        [Required]
        public string TeacherId { get; set; } = string.Empty;

        [Column("room")]
        [StringLength(50)]
        public string? Room { get; set; }

        [Column("term")]
        [Required]
        [Range(1, 3)]
        public int Term { get; set; }

        [Column("year")]
        [Required]
        [Range(2020, 2050)]
        public int Year { get; set; } = DateTime.UtcNow.Year;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // End time should be after start time
            if (EndTime <= StartTime)
            {
                result.Errors.Add("End time must be after start time");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Exam Models

    /// <summary>
    /// Exam Entity
    /// </summary>
    [Table("exams")]
    public class ExamEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("exam_id")]
        [Required]
        [StringLength(50)]
        public string ExamId { get; set; } = string.Empty;

        [Column("title")]
        [Required]
        [StringLength(255, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Column("exam_type")]
        [Required]
        [StringLength(50)]
        public string ExamType { get; set; } = string.Empty;

        [Column("subject")]
        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Column("form")]
        [Required]
        [StringLength(20)]
        public string Form { get; set; } = string.Empty;

        [Column("date")]
        [Required]
        public DateTime Date { get; set; }

        [Column("start_time")]
        [Required]
        public TimeSpan StartTime { get; set; }

        [Column("duration_minutes")]
        [Required]
        [Range(15, 300)]
        public int DurationMinutes { get; set; }

        [Column("max_marks")]
        [Required]
        [Range(1, 1000)]
        public int MaxMarks { get; set; }

        [Column("room")]
        [StringLength(50)]
        public string? Room { get; set; }

        [Column("instructions")]
        [StringLength(2000)]
        public string? Instructions { get; set; }

        [Column("term")]
        [Required]
        [Range(1, 3)]
        public int Term { get; set; }

        [Column("year")]
        [Required]
        [Range(2020, 2050)]
        public int Year { get; set; } = DateTime.UtcNow.Year;

        [Column("is_published")]
        public bool IsPublished { get; set; } = false;

        /// <summary>
        /// Calculate exam end time
        /// </summary>
        public DateTime EndTime => Date.Add(StartTime).AddMinutes(DurationMinutes);

        /// <summary>
        /// Check if exam is upcoming
        /// </summary>
        public bool IsUpcoming => Date > DateTime.UtcNow;

        /// <summary>
        /// Check if exam is completed
        /// </summary>
        public bool IsCompleted => EndTime < DateTime.UtcNow;

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate exam type
            var validExamTypes = new[] { "CAT", "Mid-Term", "End of Term", "Mock", "KCSE", "Quiz" };
            if (!validExamTypes.Contains(ExamType))
            {
                result.Errors.Add("Invalid exam type");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region System Models

    /// <summary>
    /// App Settings Entity
    /// </summary>
    [Table("app_settings")]
    public class AppSettingsEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("key")]
        [Required]
        [StringLength(100)]
        public string Key { get; set; } = string.Empty;

        [Column("value")]
        [Required]
        [StringLength(2000)]
        public string Value { get; set; } = string.Empty;

        [Column("description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Column("category")]
        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "General";

        [Column("is_public")]
        public bool IsPublic { get; set; } = false;

        [Column("updated_by")]
        public string? UpdatedBy { get; set; }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate category
            var validCategories = new[] { "General", "Academic", "Financial", "System", "Security" };
            if (!validCategories.Contains(Category))
            {
                result.Errors.Add("Invalid settings category");
                result.IsValid = false;
            }

            return result;
        }
    }

    /// <summary>
    /// Audit Log Entity
    /// </summary>
    [Table("audit_logs")]
    public class AuditLogEntity : BaseSupabaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("user_id")]
        [Required]
        public string UserId { get; set; } = string.Empty;

        [Column("action")]
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        [Column("table_name")]
        [StringLength(100)]
        public string? TableName { get; set; }

        [Column("record_id")]
        [StringLength(100)]
        public string? RecordId { get; set; }

        [Column("old_values")]
        public string? OldValues { get; set; }

        [Column("new_values")]
        public string? NewValues { get; set; }

        [Column("ip_address")]
        [StringLength(45)]
        public string? IpAddress { get; set; }

        [Column("user_agent")]
        [StringLength(500)]
        public string? UserAgent { get; set; }

        public override ValidationResult Validate()
        {
            var result = base.Validate();

            // Validate action
            var validActions = new[] { "CREATE", "UPDATE", "DELETE", "LOGIN", "LOGOUT", "VIEW" };
            if (!validActions.Contains(Action.ToUpper()))
            {
                result.Errors.Add("Invalid audit action");
                result.IsValid = false;
            }

            return result;
        }
    }

    #endregion

    #region Custom Converters and Extensions

    /// <summary>
    /// JSON converter for string arrays in Supabase
    /// </summary>
    public class JsonStringArrayConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            try
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return new List<string>();

                if (reader.TokenType == JsonTokenType.StartArray)
                {
                    var list = new List<string>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;

                        if (reader.TokenType == JsonTokenType.String)
                        {
                            var value = reader.GetString();
                            if (!string.IsNullOrEmpty(value))
                                list.Add(value);
                        }
                    }
                    return list;
                }

                // Handle case where it's stored as a JSON string
                if (reader.TokenType == JsonTokenType.String)
                {
                    var jsonString = reader.GetString();
                    if (string.IsNullOrEmpty(jsonString))
                        return new List<string>();

                    try
                    {
                        return JsonSerializer.Deserialize<List<string>>(jsonString, options) ?? new List<string>();
                    }
                    catch (JsonException)
                    {
                        // If it's not valid JSON, treat as single string
                        return new List<string> { jsonString };
                    }
                }

                return new List<string>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing string array: {ex.Message}");
                return new List<string>();
            }
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            try
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }

                JsonSerializer.Serialize(writer, value, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error serializing string array: {ex.Message}");
                writer.WriteNullValue();
            }
        }
    }

    /// <summary>
    /// Extension methods for entity models
    /// </summary>
    public static class EntityExtensions
    {
        /// <summary>
        /// Calculate grade from percentage using Kenyan system
        /// </summary>
        public static string CalculateGrade(this decimal percentage)
        {
            return percentage switch
            {
                >= 80 => "A",
                >= 75 => "A-",
                >= 70 => "B+",
                >= 65 => "B",
                >= 60 => "B-",
                >= 55 => "C+",
                >= 50 => "C",
                >= 45 => "C-",
                >= 40 => "D+",
                >= 35 => "D",
                >= 30 => "D-",
                _ => "E"
            };
        }

        /// <summary>
        /// Get points from grade using Kenyan system
        /// </summary>
        public static decimal GetPoints(this string grade)
        {
            return grade?.ToUpper() switch
            {
                "A" => 12,
                "A-" => 11,
                "B+" => 10,
                "B" => 9,
                "B-" => 8,
                "C+" => 7,
                "C" => 6,
                "C-" => 5,
                "D+" => 4,
                "D" => 3,
                "D-" => 2,
                "E" => 1,
                _ => 0
            };
        }

        /// <summary>
        /// Validate entity and throw exception if invalid
        /// </summary>
        public static void ValidateAndThrow(this BaseSupabaseModel entity)
        {
            var validationResult = entity.Validate();
            if (!validationResult.IsValid)
            {
                throw new ValidationException($"Validation failed: {validationResult.ErrorMessage}");
            }
        }

        /// <summary>
        /// Safe string truncation
        /// </summary>
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= maxLength ? value : value[..maxLength];
        }

        /// <summary>
        /// Format phone number for Kenya
        /// </summary>
        public static string FormatKenyanPhone(this string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return string.Empty;

            // Remove all non-digit characters
            var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());

            // Handle different formats
            if (digits.StartsWith("254") && digits.Length == 12)
                return $"+{digits}"; // +254xxxxxxxxx

            if (digits.StartsWith("0") && digits.Length == 10)
                return $"+254{digits[1..]}"; // Convert 0xxxxxxxxx to +254xxxxxxxxx

            if (digits.Length == 9)
                return $"+254{digits}"; // Add country code

            return phoneNumber; // Return original if format not recognized
        }
    }

    /// <summary>
    /// Custom validation exception
    /// </summary>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }

    #endregion
}