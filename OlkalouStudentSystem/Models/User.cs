// ===============================
// Models/Models.cs - Updated with Missing Properties
// ===============================
using System.Collections.ObjectModel;

namespace OlkalouStudentSystem.Models
{
    // ===== USER MANAGEMENT MODELS =====

    public enum UserType
    {
        Student,
        Teacher,
        Principal,
        DeputyPrincipal,
        Secretary,
        Bursar,
        Librarian,
        LabTechnician,
        ComputerLabTechnician,
        Priest,
        Cook,
        Gardener,
        BoardingMaster,
        NonTeachingStaff
    }

    public enum EmployeeType
    {
        BOM, // Board of Management
        NTSC, // National Teacher Service Commission
        Contract,
        Volunteer,
        TeachingPractice
    }

    public enum StudentStatus
    {
        Current,
        Graduated,
        Alumni,
        Repeating,
        Transferred,
        Dropped,
        Suspended
    }

    public enum AccountStatus
    {
        Active,
        Inactive,
        Suspended,
        Graduated,
        Completed
    }

    // Base model classes
    public class Student
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty; // Form1, Form2, Form3, Form4
        public string Class { get; set; } = string.Empty; // S, N, C, etc.
        public string StreamName { get; set; } = string.Empty; // Full class name like "Form1S"
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string ParentEmail { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime AdmissionDate { get; set; }
        public StudentStatus Status { get; set; } = StudentStatus.Current;
        public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
        public int AcademicYear { get; set; } = DateTime.Now.Year;
        public List<string> SelectedSubjects { get; set; } = new List<string>();
        public bool HasSelectedSubjects { get; set; } = false;
        public DateTime? GraduationDate { get; set; }
        public string RegistrationStatus { get; set; } = "Complete"; // Complete, Pending
        public string GuardianName { get; set; } = string.Empty;
        public string GuardianPhone { get; set; } = string.Empty;
        public string GuardianRelationship { get; set; } = string.Empty;
        public string PreviousSchool { get; set; } = string.Empty;
        public string KCSEIndexNumber { get; set; } = string.Empty;
        public string Nationality { get; set; } = "Kenyan";
        public string Religion { get; set; } = string.Empty;
        public string MedicalInfo { get; set; } = string.Empty;
    }

    public class Teacher
    {
        public string TeacherId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty; // Added missing property
        public EmployeeType EmployeeType { get; set; } = EmployeeType.BOM;
        public string NTSCNumber { get; set; } = string.Empty; // For NTSC teachers
        public string KRAPinNumber { get; set; } = string.Empty;
        public List<string> QualifiedSubjects { get; set; } = new List<string>(); // Added missing property
        public List<string> AssignedSubjects { get; set; } = new List<string>();
        public List<string> AssignedClasses { get; set; } = new List<string>();
        public string ClassTeacherFor { get; set; } = string.Empty; // If assigned as class teacher
        public DateTime HireDate { get; set; }
        public DateTime DateJoined { get; set; } = DateTime.Now; // Added missing property
        public DateTime? ContractEndDate { get; set; } // For teaching practice and contract teachers
        public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
        public decimal MonthlySalary { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string NextOfKin { get; set; } = string.Empty;
        public string NextOfKinPhone { get; set; } = string.Empty;
        public string Qualifications { get; set; } = string.Empty;
        public bool IsClassTeacher { get; set; } = false;
        public string Department { get; set; } = string.Empty;
        public List<string> Subjects { get; set; } = new List<string>();
    }

    public class Staff
    {
        public string StaffId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string EmployeeNumber { get; set; } = string.Empty; // Added missing property
        public string Position { get; set; } = string.Empty; // Changed from UserType to string
        public string JobTitle { get; set; } = string.Empty;
        public DateTime HireDate { get; set; }
        public DateTime DateJoined { get; set; } = DateTime.Now; // Added missing property
        public AccountStatus AccountStatus { get; set; } = AccountStatus.Active;
        public decimal MonthlySalary { get; set; }
        public string BankAccount { get; set; } = string.Empty;
        public string IdNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string NextOfKin { get; set; } = string.Empty;
        public string NextOfKinPhone { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }

    // ===== ACADEMIC MODELS =====

    public class SchoolClass
    {
        public string ClassId { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty; // Form1, Form2, Form3, Form4
        public string Stream { get; set; } = string.Empty; // S, N, C
        public string ClassName { get; set; } = string.Empty; // Form1S, Form2N
        public string ClassTeacherId { get; set; } = string.Empty;
        public string ClassTeacherName { get; set; } = string.Empty;
        public int Capacity { get; set; } = 45;
        public int CurrentEnrollment { get; set; } = 0;
        public int AcademicYear { get; set; } = DateTime.Now.Year;
        public bool IsActive { get; set; } = true;
        public List<string> Students { get; set; } = new List<string>();
    }

    public class Subject
    {
        public string SubjectId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string SubjectCode { get; set; } = string.Empty;
        public bool IsCompulsory { get; set; } = true;
        public bool IsOptional { get; set; } = false;
        public string Category { get; set; } = string.Empty; // Sciences, Languages, Humanities, etc.
        public List<string> FormsOffered { get; set; } = new List<string>(); // Form1, Form2, etc.
        public bool RequiresSelection { get; set; } = false; // For Form 2 2nd term selections
        public string SelectionGroup { get; set; } = string.Empty; // Business/Agriculture, History/Geography, etc.
        public bool CountsForGrading { get; set; } = true; // Computer Studies exception
        public int MaxSubjectsAllowed { get; set; } = 8;
    }

    public class SubjectAssignment
    {
        public string AssignmentId { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int AcademicYear { get; set; } = DateTime.Now.Year;
        public string Term { get; set; } = string.Empty; // Term1, Term2, Term3
        public bool IsActive { get; set; } = true;
    }

    public class Assignment
    {
        public string AssignmentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime DateCreated { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Submitted, Graded
        public string FilePath { get; set; } = string.Empty;
        public string SubmissionPath { get; set; } = string.Empty;
        public int MaxMarks { get; set; }
        public int ObtainedMarks { get; set; }
        public string TeacherComments { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public List<string> TargetClasses { get; set; } = new List<string>();
        public bool IsSubmitted { get; set; }
        public DateTime? SubmissionDate { get; set; }
        public int AcademicYear { get; set; } = DateTime.Now.Year;
        public string Term { get; set; } = string.Empty;
    }

    // ===== MARKS AND GRADING MODELS =====

    public class MarkEntry
    {
        public string MarkId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public string SubjectId { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int AcademicYear { get; set; } = DateTime.Now.Year;
        public string Term { get; set; } = string.Empty;
        public double OpeningMarks { get; set; } = 0;
        public double MidTermMarks { get; set; } = 0;
        public double EndTermMarks { get; set; } = 0;
        public double TotalMarks { get; set; } = 0;
        public string Grade { get; set; } = string.Empty;
        public int Position { get; set; } = 0;
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public DateTime DateEntered { get; set; } = DateTime.Now;
        public DateTime? DateModified { get; set; }
        public bool IsSubmitted { get; set; } = false;
        public bool IsApproved { get; set; } = false;
        public string Comments { get; set; } = string.Empty;
    }

    public class GradingScheme
    {
        public string SchemeId { get; set; } = string.Empty;
        public string SchemeName { get; set; } = string.Empty;
        public double OpeningPercentage { get; set; } = 15; // Default 15%
        public double MidTermPercentage { get; set; } = 15; // Default 15%
        public double EndTermPercentage { get; set; } = 70; // Default 70%
        public bool IsAlternativeScheme { get; set; } = false; // For (Opening+MidTerm)/2 + EndTerm/2
        public int AcademicYear { get; set; } = DateTime.Now.Year;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class ClassPerformance
    {
        public string ClassId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int AcademicYear { get; set; } = DateTime.Now.Year;
        public string Term { get; set; } = string.Empty;
        public double ClassMean { get; set; } = 0;
        public int TotalStudents { get; set; } = 0;
        public Dictionary<string, double> SubjectMeans { get; set; } = new Dictionary<string, double>();
        public List<StudentPerformance> StudentRankings { get; set; } = new List<StudentPerformance>();
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
    }

    public class StudentPerformance
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public double TotalMarks { get; set; } = 0;
        public double MeanScore { get; set; } = 0;
        public int OverallPosition { get; set; } = 0;
        public string OverallGrade { get; set; } = string.Empty;
        public List<SubjectPerformance> SubjectPerformances { get; set; } = new List<SubjectPerformance>();
        public string Term { get; set; } = string.Empty;
        public int AcademicYear { get; set; } = DateTime.Now.Year;
    }

    public class SubjectPerformance
    {
        public string SubjectId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int CurrentGrade { get; set; } = 0;
        public int PreviousGrade { get; set; } = 0;
        public string Trend { get; set; } = string.Empty; // "up", "down", "stable"
        public DateTime LastExamDate { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public List<ExamResult> ExamHistory { get; set; } = new List<ExamResult>();
        public double Marks { get; set; } = 0;
        public string Grade { get; set; } = string.Empty;
        public int Position { get; set; } = 0;
    }

    public class ExamResult
    {
        public string ExamId { get; set; } = string.Empty;
        public string ExamName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int Marks { get; set; } = 0;
        public int MaxMarks { get; set; } = 100;
        public double Percentage => MaxMarks > 0 ? (double)Marks / MaxMarks * 100 : 0;
        public string Grade { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; }
        public string Term { get; set; } = string.Empty;
        public int Year { get; set; } = DateTime.Now.Year;
    }

    // ===== DISCIPLINARY MODELS =====

    public class DisciplinaryRecord
    {
        public string RecordId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public string OffenseType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateOfOffense { get; set; }
        public string ActionTaken { get; set; } = string.Empty;
        public string ReportedBy { get; set; } = string.Empty;
        public string HandledBy { get; set; } = string.Empty; // Usually Deputy Principal
        public string Status { get; set; } = "Open"; // Open, Resolved, Escalated
        public string Severity { get; set; } = "Minor"; // Minor, Moderate, Major, Severe
        public DateTime? ResolutionDate { get; set; }
        public string ParentNotified { get; set; } = "No"; // Yes, No, Pending
        public DateTime? ParentNotificationDate { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

    // ===== FINANCIAL MODELS =====

    public class FeesInfo
    {
        public string StudentId { get; set; } = string.Empty;
        public decimal TotalFees { get; set; } = 0;
        public decimal PaidAmount { get; set; } = 0;
        public decimal Balance { get; set; } = 0;
        public decimal PendingAmount => Balance > 0 ? Balance : 0;
        public DateTime? LastPaymentDate { get; set; }
        public string PaymentStatus { get; set; } = "Pending"; // Paid, Pending, Overdue
        public List<PaymentRecord> PaymentHistory { get; set; } = new List<PaymentRecord>();
        public DateTime DueDate { get; set; }
        public bool IsOverdue => DateTime.Now > DueDate && Balance > 0;
        public string AcademicYear { get; set; } = $"{DateTime.Now.Year}/{DateTime.Now.Year + 1}";
        public string Form { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;

        public void CalculateBalance()
        {
            Balance = TotalFees - PaidAmount;
        }
    }

    public class PaymentRecord
    {
        public string PaymentId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0;
        public DateTime PaymentDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = true;
        public string Status { get; set; } = "Approved";
        public string AcademicYear { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public string ReceiptImagePath { get; set; } = string.Empty; // For scanned receipts

        // Calculated properties for UI binding
        public bool CanDownloadReceipt => IsApproved && !string.IsNullOrEmpty(ReceiptNumber);
        public string StatusText => IsApproved ? "Approved" : "Pending";
        public string AmountFormatted => $"KSh {Amount:N2}";
        public string PaymentDateFormatted => PaymentDate.ToString("MMM dd, yyyy");
    }

    public class FeesStructure
    {
        public string Id { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string AcademicYear { get; set; } = string.Empty;
        public decimal TuitionFees { get; set; } = 0;
        public decimal BoardingFees { get; set; } = 0;
        public decimal ExamFees { get; set; } = 0;
        public decimal ActivityFees { get; set; } = 0;
        public decimal UniformFees { get; set; } = 0;
        public decimal BooksFees { get; set; } = 0;
        public decimal OtherFees { get; set; } = 0;
        public decimal TotalFees { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;

        public void CalculateTotalFees()
        {
            TotalFees = TuitionFees + BoardingFees + ExamFees + ActivityFees + UniformFees + BooksFees + OtherFees;
        }
    }

    public class SalaryPayment
    {
        public string PaymentId { get; set; } = string.Empty;
        public string EmployeeId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public UserType EmployeeType { get; set; } = UserType.Teacher;
        public decimal BasicSalary { get; set; } = 0;
        public decimal Allowances { get; set; } = 0;
        public decimal Deductions { get; set; } = 0;
        public decimal NetSalary { get; set; } = 0;
        public string PayrollMonth { get; set; } = string.Empty;
        public int PayrollYear { get; set; } = DateTime.Now.Year;
        public DateTime PaymentDate { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Paid
        public string ApprovedBy { get; set; } = string.Empty;
        public string PaidBy { get; set; } = string.Empty; // Usually Bursar
        public DateTime? ApprovalDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string BankReference { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    // ===== FINANCIAL ACCOUNTING MODELS =====

    public class JournalEntry
    {
        public string EntryId { get; set; } = string.Empty;
        public DateTime TransactionDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public List<JournalEntryLine> Lines { get; set; } = new List<JournalEntryLine>();
        public decimal TotalDebit { get; set; } = 0;
        public decimal TotalCredit { get; set; } = 0;
        public string Status { get; set; } = "Draft"; // Draft, Posted, Approved
        public string CreatedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ApprovalDate { get; set; }
        public bool IsBalanced => TotalDebit == TotalCredit;
    }

    public class JournalEntryLine
    {
        public string LineId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal DebitAmount { get; set; } = 0;
        public decimal CreditAmount { get; set; } = 0;
    }

    public class ChartOfAccounts
    {
        public string AccountId { get; set; } = string.Empty;
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty; // Asset, Liability, Equity, Revenue, Expense
        public string ParentAccountId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public decimal Balance { get; set; } = 0;
        public string Description { get; set; } = string.Empty;
    }

    public class Invoice
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string VendorId { get; set; } = string.Empty;
        public string VendorName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; } = 0;
        public decimal PaidAmount { get; set; } = 0;
        public decimal Balance => Amount - PaidAmount;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Paid, Cancelled
        public string Category { get; set; } = string.Empty; // Utilities, Supplies, Maintenance, etc.
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string PaidBy { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }
        public string AttachmentPath { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    // ===== LIBRARY MODELS =====

    public class LibraryBook
    {
        public string BookId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string ISBN { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public int PublicationYear { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime DateAdded { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsAvailable => AvailableCopies > 0;
        public string Subject { get; set; } = string.Empty; // For curriculum books
        public string Form { get; set; } = string.Empty; // Target form level
    }

    public class BookIssue
    {
        public string IssueId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string BookId { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = "Issued"; // Issued, Returned, Overdue, Lost
        public decimal? FineAmount { get; set; }
        public bool IsOverdue => DateTime.Now > DueDate && ReturnDate == null;
        public int DaysOverdue => IsOverdue ? (DateTime.Now - DueDate).Days : 0;
        public string IssuedBy { get; set; } = string.Empty; // Librarian name
        public string ReturnedTo { get; set; } = string.Empty;
    }

    // ===== ACTIVITIES AND NOTIFICATIONS =====

    public class Activity
    {
        public string ActivityId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty; // Academic, Sports, Cultural, etc.
        public string Organizer { get; set; } = string.Empty;
        public bool IsOptional { get; set; } = true;
        public string Requirements { get; set; } = string.Empty;
        public List<string> TargetForms { get; set; } = new List<string>();
        public string Status { get; set; } = "Scheduled"; // Scheduled, Ongoing, Completed, Cancelled

        // Additional properties for better functionality
        public DateTime? RegistrationDeadline { get; set; }
        public int? MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; } = 0;

        // Calculated properties for UI
        public bool IsUpcoming => Date > DateTime.Now && Status != "Cancelled";
        public bool IsPast => Date < DateTime.Now;
        public bool CanRegister => IsOptional &&
                                  RegistrationDeadline.HasValue &&
                                  RegistrationDeadline > DateTime.Now &&
                                  (!MaxParticipants.HasValue || CurrentParticipants < MaxParticipants.Value) &&
                                  Status == "Scheduled";

        // Formatted properties for display
        public string DateFormatted => Date.ToString("MMM dd, yyyy");
        public string TimeFormatted => $"{StartTime:HH:mm} - {EndTime:HH:mm}";
        public string ParticipantsText => MaxParticipants.HasValue
            ? $"{CurrentParticipants}/{MaxParticipants}"
            : CurrentParticipants.ToString();
    }

    public class Achievement
    {
        public string AchievementId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // Academic, Sports, Leadership, etc.
        public DateTime DateAchieved { get; set; } = DateTime.Now; // Added missing property
        public DateTime Date { get; set; } = DateTime.Now; // Keep for backward compatibility
        public string AwardedBy { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty; // School, County, National, etc.
        public string CertificatePath { get; set; } = string.Empty;
        public int Points { get; set; } = 0; // Added missing property
    }

    public class Notification
    {
        public string NotificationId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public string RecipientType { get; set; } = string.Empty; // Student, Teacher, Staff, etc.
        public string NotificationType { get; set; } = string.Empty; // Payment, Assignment, Disciplinary, etc.
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public bool IsRead { get; set; } = false;
        public string Priority { get; set; } = "Normal"; // Low, Normal, High, Urgent
        public string ActionUrl { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
    }

    // ===== DASHBOARD AND ANALYTICS MODELS =====

    public class DashboardData
    {
        public FeesInfo CurrentFees { get; set; } = new FeesInfo();
        public ObservableCollection<Assignment> PendingAssignments { get; set; } = new ObservableCollection<Assignment>();
        public ObservableCollection<LibraryBook> IssuedBooks { get; set; } = new ObservableCollection<LibraryBook>();
        public ObservableCollection<Achievement> RecentAchievements { get; set; } = new ObservableCollection<Achievement>();
        public ObservableCollection<Activity> UpcomingActivities { get; set; } = new ObservableCollection<Activity>();
        public ObservableCollection<SubjectPerformance> AcademicPerformance { get; set; } = new ObservableCollection<SubjectPerformance>();
        public ObservableCollection<DisciplinaryRecord> RecentDisciplinaryRecords { get; set; } = new ObservableCollection<DisciplinaryRecord>();
        public ObservableCollection<Notification> RecentNotifications { get; set; } = new ObservableCollection<Notification>();
    }

    public class TeacherDashboardData
    {
        public List<SchoolClass> AssignedClasses { get; set; } = new List<SchoolClass>();
        public List<Subject> AssignedSubjects { get; set; } = new List<Subject>();
        public SchoolClass ClassTeacherFor { get; set; } = null;
        public List<MarkEntry> PendingMarks { get; set; } = new List<MarkEntry>();
        public List<Assignment> MyAssignments { get; set; } = new List<Assignment>();
        public Dictionary<string, double> SubjectPerformanceStats { get; set; } = new Dictionary<string, double>();
        public List<Student> MyStudents { get; set; } = new List<Student>();
    }

    public class AdminDashboardData
    {
        public int TotalStudents { get; set; } = 0;
        public int TotalTeachers { get; set; } = 0;
        public int TotalStaff { get; set; } = 0;
        public decimal TotalFeesCollected { get; set; } = 0;
        public decimal PendingFees { get; set; } = 0;
        public int PendingApprovals { get; set; } = 0;
        public List<SalaryPayment> PendingSalaries { get; set; } = new List<SalaryPayment>();
        public List<Invoice> PendingInvoices { get; set; } = new List<Invoice>();
        public Dictionary<string, int> EnrollmentByForm { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, double> PerformanceByForm { get; set; } = new Dictionary<string, double>();
    }

    // ===== SYSTEM CONFIGURATION MODELS =====

    public class SystemSettings
    {
        public string SettingId { get; set; } = string.Empty;
        public string SettingKey { get; set; } = string.Empty;
        public string SettingValue { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public string ModifiedBy { get; set; } = string.Empty;
    }

    public class AcademicYear
    {
        public string YearId { get; set; } = string.Empty;
        public int StartYear { get; set; } = DateTime.Now.Year;
        public int EndYear { get; set; } = DateTime.Now.Year + 1;
        public string YearName => $"{StartYear}/{EndYear}";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsComplete { get; set; } = false;
        public List<Term> Terms { get; set; } = new List<Term>();
    }

    public class Term
    {
        public string TermId { get; set; } = string.Empty;
        public string TermName { get; set; } = string.Empty; // Term 1, Term 2, Term 3
        public int TermNumber { get; set; } = 1;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; } = false;
        public bool IsComplete { get; set; } = false;
        public string AcademicYearId { get; set; } = string.Empty;
    }

    // ===== API RESPONSE MODELS =====

    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T Data { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
    }

    public class LoginRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public Student Student { get; set; }
        public Teacher Teacher { get; set; }
        public Staff Staff { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
    }

    // ===== REGISTRATION MODELS =====

    public class StudentRegistration
    {
        public string RegistrationId { get; set; } = string.Empty;
        public Student StudentInfo { get; set; } = new Student();
        public string RegistrationStatus { get; set; } = "Pending"; // Pending, Complete, Incomplete
        public List<string> MissingFields { get; set; } = new List<string>();
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public string RegisteredBy { get; set; } = string.Empty; // Secretary
        public DateTime? CompletionDate { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

    public class TeacherRegistration
    {
        public string RegistrationId { get; set; } = string.Empty;
        public Teacher TeacherInfo { get; set; } = new Teacher();
        public string RegistrationStatus { get; set; } = "Pending";
        public List<string> MissingFields { get; set; } = new List<string>();
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public string RegisteredBy { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
        public bool RequiresApproval => TeacherInfo.EmployeeType == EmployeeType.BOM;
    }

    public class StaffRegistration
    {
        public string RegistrationId { get; set; } = string.Empty;
        public Staff StaffInfo { get; set; } = new Staff();
        public string RegistrationStatus { get; set; } = "Pending";
        public List<string> MissingFields { get; set; } = new List<string>();
        public DateTime RegistrationDate { get; set; } = DateTime.Now;
        public string RegisteredBy { get; set; } = string.Empty;
        public DateTime? ApprovalDate { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    // ===== FILE UPLOAD MODELS =====

    public class FileData
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public byte[] Data { get; set; }
        public long Size { get; set; }
        public string FileExtension { get; set; } = string.Empty;
        public bool IsImage { get; set; } = false;
        public string Quality { get; set; } = string.Empty; // For image quality (HDR, etc.)
    }

    public class UploadResult
    {
        public bool Success { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public string UploadedBy { get; set; } = string.Empty;
    }

    // ===== SUBJECT SELECTION MODELS =====

    public class SubjectSelection
    {
        public string SelectionId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string Term { get; set; } = string.Empty;
        public int AcademicYear { get; set; } = DateTime.Now.Year;
        public List<string> CompulsorySubjects { get; set; } = new List<string>
        {
            "Mathematics", "English", "Kiswahili", "Chemistry", "CRE"
        };
        public List<string> SelectedOptionalSubjects { get; set; } = new List<string>();
        public List<string> AvailableOptionalSubjects { get; set; } = new List<string>();
        public int MaxSubjects { get; set; } = 8;
        public bool IsValid => (CompulsorySubjects.Count + SelectedOptionalSubjects.Count) <= MaxSubjects;
        public string SelectionStatus { get; set; } = "Pending"; // Pending, Submitted, Approved
        public DateTime SelectionDate { get; set; } = DateTime.Now;
        public DateTime? SubmissionDate { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;
    }

    public class OptionalSubjectGroup
    {
        public string GroupName { get; set; } = string.Empty;
        public List<string> Subjects { get; set; } = new List<string>();
        public int MaxSelections { get; set; } = 1;
        public bool IsRequired { get; set; } = true;
    }

    // ===== REPORT MODELS =====

    public class ReportTemplate
    {
        public string TemplateId { get; set; } = string.Empty;
        public string TemplateName { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty; // Academic, Financial, Administrative
        public string Description { get; set; } = string.Empty;
        public string TemplateContent { get; set; } = string.Empty; // HTML template
        public List<string> RequiredParameters { get; set; } = new List<string>();
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class ReportRequest
    {
        public string RequestId { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public string RequestedBy { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending"; // Pending, Processing, Complete, Failed
        public string OutputFilePath { get; set; } = string.Empty;
        public DateTime? CompletionDate { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    // ===== ADDITIONAL USER PROFILE MODELS =====

    public class StudentProfile
    {
        public string StudentId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string ProfileImageUrl { get; set; } = string.Empty;
        public DateTime LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
        public NotificationSettings NotificationSettings { get; set; } = new NotificationSettings();
    }

    public class NotificationSettings
    {
        public bool AssignmentNotifications { get; set; } = true;
        public bool ActivityNotifications { get; set; } = true;
        public bool FeeNotifications { get; set; } = true;
        public bool LibraryNotifications { get; set; } = true;
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool DisciplinaryNotifications { get; set; } = true;
        public bool MarksNotifications { get; set; } = true;
    }

    // ===== AUDIT AND LOGGING MODELS =====

    public class AuditLog
    {
        public string LogId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
        public string OldValues { get; set; } = string.Empty; // JSON
        public string NewValues { get; set; } = string.Empty; // JSON
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string IPAddress { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    // ===== MENU AND NAVIGATION MODELS =====

    public class MenuItemModel
    {
        public string Title { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public bool IsLogout { get; set; } = false;
        public bool IsVisible { get; set; } = true;
        public List<string> RequiredRoles { get; set; } = new List<string>();
        public int SortOrder { get; set; } = 0;
    }

    // ===== PROMOTION AND CLASS ADVANCEMENT MODELS =====

    public class ClassPromotion
    {
        public string PromotionId { get; set; } = string.Empty;
        public int FromAcademicYear { get; set; }
        public int ToAcademicYear { get; set; }
        public DateTime PromotionDate { get; set; } = DateTime.Now;
        public string ProcessedBy { get; set; } = string.Empty;
        public List<StudentPromotion> StudentPromotions { get; set; } = new List<StudentPromotion>();
        public string Status { get; set; } = "Pending"; // Pending, Processing, Complete
        public int TotalStudents { get; set; } = 0;
        public int PromotedStudents { get; set; } = 0;
        public int GraduatedStudents { get; set; } = 0;
        public int RepeatingStudents { get; set; } = 0;
    }

    public class StudentPromotion
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public string FromForm { get; set; } = string.Empty;
        public string FromClass { get; set; } = string.Empty;
        public string ToForm { get; set; } = string.Empty;
        public string ToClass { get; set; } = string.Empty;
        public string PromotionType { get; set; } = string.Empty; // Promoted, Graduated, Repeating
        public double FinalMeanScore { get; set; } = 0;
        public bool MeetsPromotionCriteria { get; set; } = true;
        public string Comments { get; set; } = string.Empty;
        public DateTime PromotionDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Teacher Class model for teacher dashboard
    /// </summary>
    public class TeacherClass
    {
        public string ClassId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int StudentCount { get; set; }
        public string Schedule { get; set; } = string.Empty;
        public string TeacherId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Pending Mark model for teacher grading
    /// </summary>
    public class PendingMark
    {
        public string MarkId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AssignmentTitle { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; }
        public int MaxMarks { get; set; }
        public string AssignmentId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
    }

    /// <summary>
    /// School Statistics for admin dashboard
    /// </summary>
    public class SchoolStatistics
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalStaff { get; set; }
        public int ActiveClasses { get; set; }
        public decimal PendingFees { get; set; }
        public double CompletionRate { get; set; }
        public double AttendanceRate { get; set; }
        public double PassRate { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveStudents { get; set; }
    }

    /// <summary>
    /// Admin Activity model for admin dashboard
    /// </summary>
    public class AdminActivity
    {
        public string ActivityId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Registration Statistics for secretary dashboard
    /// </summary>
    public class RegistrationStatistics
    {
        public int TotalRegistrations { get; set; }
        public int NewRegistrationsThisMonth { get; set; }
        public int PendingApplications { get; set; }
        public int CompletedRegistrations { get; set; }
        public int RejectedApplications { get; set; }
        public int ActiveStudents { get; set; }
        public int ActiveTeachers { get; set; }
        public int ActiveStaff { get; set; }
    }

    /// <summary>
    /// Financial Statistics for bursar dashboard
    /// </summary>
    public class FinancialStatistics
    {
        public decimal TotalFeesCollected { get; set; }
        public decimal PendingFees { get; set; }
        public decimal MonthlyCollection { get; set; }
        public decimal ExpectedRevenue { get; set; }
        public double CollectionRate { get; set; }
        public int StudentsWithPendingFees { get; set; }
        public decimal AveragePayment { get; set; }
        public decimal TotalExpenses { get; set; }
    }

    /// <summary>
    /// Basic Statistics for general staff
    /// </summary>
    public class BasicStatistics
    {
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }
        public int ActiveClasses { get; set; }
        public double TodayAttendance { get; set; }
        public int PresentStudents { get; set; }
        public int AbsentStudents { get; set; }
    }
}