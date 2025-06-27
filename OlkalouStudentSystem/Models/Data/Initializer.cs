// ===============================
// Services/DBInitializer.cs - Complete Error-Free Database Initializer
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Models.Data;
using System.Security.Cryptography;
using System.Text;

namespace OlkalouStudentSystem.Services
{
    public class DbInitializer
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly ILogger<DbInitializer>? _logger;

        public DbInitializer(ILogger<DbInitializer>? logger = null)
        {
            _supabaseClient = SupabaseService.Instance.Client;
            _logger = logger;
        }

        /// <summary>
        /// Initialize the database with sample data
        /// </summary>
        public async Task<bool> InitializeDatabaseAsync()
        {
            try
            {
                _logger?.LogInformation("Starting database initialization...");

                // Initialize in order (dependencies first)
                await InitializeUsersAsync();
                await InitializeStudentsAsync();
                await InitializeTeachersAsync();
                await InitializeStaffAsync();
                await InitializeFeesAsync();
                await InitializeLibraryBooksAsync();
                await InitializeAssignmentsAsync();
                await InitializeActivitiesAsync();
                await InitializeNotificationsAsync();

                _logger?.LogInformation("Database initialization completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during database initialization");
                return false;
            }
        }

        #region User Initialization

        private async Task InitializeUsersAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing users...");

                var users = new List<UserEntity>
                {
                    // Students
                    new UserEntity
                    {
                        Id = "user_001",
                        PhoneNumber = "+254712345678",
                        Password = HashPassword("password123"),
                        UserType = "Student",
                        IsActive = true,
                        ProfileCompleted = true,
                        TermsAccepted = true,
                        TermsAcceptedAt = DateTime.UtcNow.AddDays(-30)
                    },
                    new UserEntity
                    {
                        Id = "user_002",
                        PhoneNumber = "+254712345679",
                        Password = HashPassword("password123"),
                        UserType = "Student",
                        IsActive = true,
                        ProfileCompleted = true,
                        TermsAccepted = true,
                        TermsAcceptedAt = DateTime.UtcNow.AddDays(-25)
                    },
                    new UserEntity
                    {
                        Id = "user_003",
                        PhoneNumber = "+254712345680",
                        Password = HashPassword("password123"),
                        UserType = "Student",
                        IsActive = true,
                        ProfileCompleted = true,
                        TermsAccepted = true,
                        TermsAcceptedAt = DateTime.UtcNow.AddDays(-20)
                    },

                    // Teachers
                    new UserEntity
                    {
                        Id = "user_101",
                        PhoneNumber = "+254722345678",
                        Password = HashPassword("teacher123"),
                        UserType = "Teacher",
                        IsActive = true,
                        ProfileCompleted = true,
                        TermsAccepted = true,
                        TermsAcceptedAt = DateTime.UtcNow.AddDays(-60)
                    },
                    new UserEntity
                    {
                        Id = "user_102",
                        PhoneNumber = "+254722345679",
                        Password = HashPassword("teacher123"),
                        UserType = "Teacher",
                        IsActive = true,
                        ProfileCompleted = true,
                        TermsAccepted = true,
                        TermsAcceptedAt = DateTime.UtcNow.AddDays(-55)
                    },

                    // Staff
                    new UserEntity
                    {
                        Id = "user_201",
                        PhoneNumber = "+254732345678",
                        Password = HashPassword("principal123"),
                        UserType = "Principal",
                        IsActive = true,
                        ProfileCompleted = true,
                        TermsAccepted = true,
                        TermsAcceptedAt = DateTime.UtcNow.AddDays(-90)
                    },
                    new UserEntity
                    {
                        Id = "user_202",
                        PhoneNumber = "+254732345679",
                        Password = HashPassword("secretary123"),
                        UserType = "Secretary",
                        IsActive = true,
                        ProfileCompleted = true,
                        TermsAccepted = true,
                        TermsAcceptedAt = DateTime.UtcNow.AddDays(-85)
                    },
                    new UserEntity
                    {
                        Id = "user_203",
                        PhoneNumber = "+254732345680",
                        Password = HashPassword("bursar123"),
                        UserType = "Bursar",
                        IsActive = true,
                        ProfileCompleted = true,
                        TermsAccepted = true,
                        TermsAcceptedAt = DateTime.UtcNow.AddDays(-80)
                    },
                    new UserEntity
                    {
                        Id = "user_204",
                        PhoneNumber = "+254732345681",
                        Password = HashPassword("librarian123"),
                        UserType = "Librarian",
                        IsActive = true,
                        ProfileCompleted = true,
                        TermsAccepted = true,
                        TermsAcceptedAt = DateTime.UtcNow.AddDays(-75)
                    }
                };

                foreach (var user in users)
                {
                    await CreateOrUpdateUserAsync(user);
                }

                _logger?.LogInformation($"Initialized {users.Count} users");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing users");
                throw;
            }
        }

        #endregion

        #region Student Initialization

        private async Task InitializeStudentsAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing students...");

                var students = new List<StudentEntity>
                {
                    new StudentEntity
                    {
                        Id = "student_001",
                        UserId = "user_001",
                        StudentId = "STU001",
                        AdmissionNo = "ADM2024001",
                        FullName = "John Doe Mwangi",
                        Form = "Form1",
                        Class = "S",
                        Email = "john.doe@student.olkalou.ac.ke",
                        DateOfBirth = new DateTime(2008, 5, 15),
                        Gender = "Male",
                        Address = "P.O. Box 123, Olkalou",
                        ParentPhone = "+254712345678",
                        GuardianName = "Mary Mwangi",
                        GuardianPhone = "+254712345677",
                        Year = DateTime.UtcNow.Year,
                        Term = 1,
                        EnrollmentDate = DateTime.UtcNow.AddDays(-30),
                        IsActive = true,
                        Status = "Active",
                        Nationality = "Kenyan",
                        Religion = "Christian"
                    },
                    new StudentEntity
                    {
                        Id = "student_002",
                        UserId = "user_002",
                        StudentId = "STU002",
                        AdmissionNo = "ADM2024002",
                        FullName = "Jane Smith Wanjiku",
                        Form = "Form2",
                        Class = "N",
                        Email = "jane.smith@student.olkalou.ac.ke",
                        DateOfBirth = new DateTime(2007, 8, 22),
                        Gender = "Female",
                        Address = "P.O. Box 456, Olkalou",
                        ParentPhone = "+254712345679",
                        GuardianName = "Peter Wanjiku",
                        GuardianPhone = "+254712345676",
                        Year = DateTime.UtcNow.Year,
                        Term = 1,
                        EnrollmentDate = DateTime.UtcNow.AddDays(-25),
                        IsActive = true,
                        Status = "Active",
                        Nationality = "Kenyan",
                        Religion = "Christian"
                    },
                    new StudentEntity
                    {
                        Id = "student_003",
                        UserId = "user_003",
                        StudentId = "STU003",
                        AdmissionNo = "ADM2024003",
                        FullName = "Michael Johnson Kariuki",
                        Form = "Form3",
                        Class = "C",
                        Email = "michael.johnson@student.olkalou.ac.ke",
                        DateOfBirth = new DateTime(2006, 12, 10),
                        Gender = "Male",
                        Address = "P.O. Box 789, Olkalou",
                        ParentPhone = "+254712345680",
                        GuardianName = "Grace Kariuki",
                        GuardianPhone = "+254712345675",
                        Year = DateTime.UtcNow.Year,
                        Term = 1,
                        EnrollmentDate = DateTime.UtcNow.AddDays(-20),
                        IsActive = true,
                        Status = "Active",
                        Nationality = "Kenyan",
                        Religion = "Christian"
                    }
                };

                foreach (var student in students)
                {
                    await CreateOrUpdateStudentAsync(student);
                }

                _logger?.LogInformation($"Initialized {students.Count} students");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing students");
                throw;
            }
        }

        #endregion

        #region Teacher Initialization

        private async Task InitializeTeachersAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing teachers...");

                var teachers = new List<TeacherEntity>
                {
                    new TeacherEntity
                    {
                        Id = "teacher_001",
                        UserId = "user_101",
                        TeacherId = "TCH001",
                        EmployeeNumber = "EMP2024001",
                        FullName = "Dr. Sarah Wanjiru",
                        EmployeeType = "BOM",
                        Email = "sarah.wanjiru@olkalou.ac.ke",
                        PhoneNumber = "+254722345678",
                        Subjects = new List<string> { "Mathematics", "Physics" },
                        QualifiedSubjects = new List<string> { "Mathematics", "Physics", "Chemistry" },
                        AssignedForms = new List<string> { "Form3", "Form4" },
                        Qualification = "PhD in Mathematics Education",
                        ExperienceYears = 8,
                        EmploymentDate = DateTime.UtcNow.AddYears(-5),
                        DateJoined = DateTime.UtcNow.AddYears(-5),
                        Department = "Mathematics",
                        IsClassTeacher = true,
                        ClassTeacherFor = "Form3C",
                        IsActive = true,
                        MonthlySalary = 85000m,
                        NationalId = "12345678",
                        BankAccount = "1234567890"
                    },
                    new TeacherEntity
                    {
                        Id = "teacher_002",
                        UserId = "user_102",
                        TeacherId = "TCH002",
                        EmployeeNumber = "EMP2024002",
                        FullName = "Mr. James Kamau",
                        EmployeeType = "NTSC",
                        TscNumber = "TSC123456",
                        Email = "james.kamau@olkalou.ac.ke",
                        PhoneNumber = "+254722345679",
                        Subjects = new List<string> { "English", "Literature" },
                        QualifiedSubjects = new List<string> { "English", "Literature", "Kiswahili" },
                        AssignedForms = new List<string> { "Form1", "Form2" },
                        Qualification = "Bachelor of Arts in English Literature",
                        ExperienceYears = 6,
                        EmploymentDate = DateTime.UtcNow.AddYears(-4),
                        DateJoined = DateTime.UtcNow.AddYears(-4),
                        Department = "Languages",
                        IsClassTeacher = true,
                        ClassTeacherFor = "Form1S",
                        IsActive = true,
                        MonthlySalary = 75000m,
                        NationalId = "23456789",
                        BankAccount = "2345678901"
                    }
                };

                foreach (var teacher in teachers)
                {
                    await CreateOrUpdateTeacherAsync(teacher);
                }

                _logger?.LogInformation($"Initialized {teachers.Count} teachers");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing teachers");
                throw;
            }
        }

        #endregion

        #region Staff Initialization

        private async Task InitializeStaffAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing staff...");

                var staff = new List<StaffEntity>
                {
                    new StaffEntity
                    {
                        Id = "staff_001",
                        UserId = "user_201",
                        StaffId = "STF001",
                        EmployeeNumber = "EMP2024101",
                        FullName = "Dr. Margaret Njeri",
                        Position = "Principal",
                        Department = "Administration",
                        Email = "principal@olkalou.ac.ke",
                        PhoneNumber = "+254732345678",
                        EmploymentDate = DateTime.UtcNow.AddYears(-10),
                        DateJoined = DateTime.UtcNow.AddYears(-10),
                        Qualification = "PhD in Educational Leadership",
                        Salary = 150000m,
                        OfficeLocation = "Administration Block",
                        IsActive = true,
                        Permissions = new List<string> { "all_access", "user_management", "financial_management" },
                        NationalId = "34567890",
                        BankAccount = "3456789012"
                    },
                    new StaffEntity
                    {
                        Id = "staff_002",
                        UserId = "user_202",
                        StaffId = "STF002",
                        EmployeeNumber = "EMP2024102",
                        FullName = "Ms. Grace Wambui",
                        Position = "Secretary",
                        Department = "Administration",
                        Email = "secretary@olkalou.ac.ke",
                        PhoneNumber = "+254732345679",
                        EmploymentDate = DateTime.UtcNow.AddYears(-3),
                        DateJoined = DateTime.UtcNow.AddYears(-3),
                        Qualification = "Diploma in Secretarial Studies",
                        Salary = 45000m,
                        OfficeLocation = "Reception",
                        IsActive = true,
                        Permissions = new List<string> { "student_registration", "document_management" },
                        NationalId = "45678901",
                        BankAccount = "4567890123"
                    },
                    new StaffEntity
                    {
                        Id = "staff_003",
                        UserId = "user_203",
                        StaffId = "STF003",
                        EmployeeNumber = "EMP2024103",
                        FullName = "Mr. Peter Kiprotich",
                        Position = "Bursar",
                        Department = "Finance",
                        Email = "bursar@olkalou.ac.ke",
                        PhoneNumber = "+254732345680",
                        EmploymentDate = DateTime.UtcNow.AddYears(-5),
                        DateJoined = DateTime.UtcNow.AddYears(-5),
                        Qualification = "Bachelor of Commerce (Accounting)",
                        Salary = 80000m,
                        OfficeLocation = "Finance Office",
                        IsActive = true,
                        Permissions = new List<string> { "financial_management", "fees_management", "payroll" },
                        NationalId = "56789012",
                        BankAccount = "5678901234"
                    },
                    new StaffEntity
                    {
                        Id = "staff_004",
                        UserId = "user_204",
                        StaffId = "STF004",
                        EmployeeNumber = "EMP2024104",
                        FullName = "Mrs. Alice Nyambura",
                        Position = "Librarian",
                        Department = "Library",
                        Email = "librarian@olkalou.ac.ke",
                        PhoneNumber = "+254732345681",
                        EmploymentDate = DateTime.UtcNow.AddYears(-2),
                        DateJoined = DateTime.UtcNow.AddYears(-2),
                        Qualification = "Bachelor of Library and Information Science",
                        Salary = 50000m,
                        OfficeLocation = "Library",
                        IsActive = true,
                        Permissions = new List<string> { "library_management", "book_issue" },
                        NationalId = "67890123",
                        BankAccount = "6789012345"
                    }
                };

                foreach (var staffMember in staff)
                {
                    await CreateOrUpdateStaffAsync(staffMember);
                }

                _logger?.LogInformation($"Initialized {staff.Count} staff members");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing staff");
                throw;
            }
        }

        #endregion

        #region Fees Initialization

        private async Task InitializeFeesAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing fees...");

                var fees = new List<FeesEntity>
                {
                    new FeesEntity
                    {
                        Id = "fees_001",
                        StudentId = "student_001",
                        TotalFees = 85000m,
                        PaidAmount = 30000m,
                        Balance = 55000m,
                        Year = DateTime.UtcNow.Year,
                        Term = 1,
                        DueDate = DateTime.UtcNow.AddDays(30),
                        PaymentStatus = "Pending",
                        LastPaymentDate = DateTime.UtcNow.AddDays(-15)
                    },
                    new FeesEntity
                    {
                        Id = "fees_002",
                        StudentId = "student_002",
                        TotalFees = 85000m,
                        PaidAmount = 85000m,
                        Balance = 0m,
                        Year = DateTime.UtcNow.Year,
                        Term = 1,
                        DueDate = DateTime.UtcNow.AddDays(30),
                        PaymentStatus = "Paid",
                        LastPaymentDate = DateTime.UtcNow.AddDays(-10)
                    },
                    new FeesEntity
                    {
                        Id = "fees_003",
                        StudentId = "student_003",
                        TotalFees = 85000m,
                        PaidAmount = 50000m,
                        Balance = 35000m,
                        Year = DateTime.UtcNow.Year,
                        Term = 1,
                        DueDate = DateTime.UtcNow.AddDays(30),
                        PaymentStatus = "Partial",
                        LastPaymentDate = DateTime.UtcNow.AddDays(-5)
                    }
                };

                foreach (var fee in fees)
                {
                    await CreateOrUpdateFeesAsync(fee);
                }

                // Initialize payment records
                var payments = new List<FeesPaymentEntity>
                {
                    new FeesPaymentEntity
                    {
                        Id = "payment_001",
                        StudentId = "student_001",
                        Amount = 30000m,
                        PaymentMethod = "M-Pesa",
                        ReceiptNumber = "RCP202400001",
                        TransactionId = "MPX123456789",
                        IsApproved = true,
                        PaymentDate = DateTime.UtcNow.AddDays(-15),
                        Description = "Term 1 Fees Payment",
                        ReceivedBy = "Bursar Office"
                    },
                    new FeesPaymentEntity
                    {
                        Id = "payment_002",
                        StudentId = "student_002",
                        Amount = 85000m,
                        PaymentMethod = "Bank Transfer",
                        ReceiptNumber = "RCP202400002",
                        TransactionId = "BNK987654321",
                        IsApproved = true,
                        PaymentDate = DateTime.UtcNow.AddDays(-10),
                        Description = "Full Term 1 Fees Payment",
                        ReceivedBy = "Bursar Office",
                        BankName = "Equity Bank",
                        AccountNumber = "0123456789"
                    }
                };

                foreach (var payment in payments)
                {
                    await CreateOrUpdatePaymentAsync(payment);
                }

                _logger?.LogInformation($"Initialized {fees.Count} fees records and {payments.Count} payments");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing fees");
                throw;
            }
        }

        #endregion

        #region Library Books Initialization

        private async Task InitializeLibraryBooksAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing library books...");

                var books = new List<LibraryBookEntity>
                {
                    new LibraryBookEntity
                    {
                        Id = "book_001",
                        BookId = "BK001",
                        Title = "Mathematics Form 1",
                        Author = "KIE Publishers",
                        ISBN = "978-9966-25-123-4",
                        Category = "Textbook",
                        Subcategory = "Mathematics",
                        Publisher = "Kenya Institute of Education",
                        PublicationYear = 2023,
                        Language = "English",
                        TotalCopies = 50,
                        AvailableCopies = 45,
                        DamagedCopies = 2,
                        LostCopies = 3,
                        Description = "Form 1 Mathematics textbook covering algebra, geometry, and statistics"
                    },
                    new LibraryBookEntity
                    {
                        Id = "book_002",
                        BookId = "BK002",
                        Title = "English Grammar in Use",
                        Author = "Raymond Murphy",
                        ISBN = "978-0-521-53762-9",
                        Category = "Reference",
                        Subcategory = "English",
                        Publisher = "Cambridge University Press",
                        PublicationYear = 2019,
                        Language = "English",
                        TotalCopies = 30,
                        AvailableCopies = 28,
                        DamagedCopies = 1,
                        LostCopies = 1,
                        Description = "Comprehensive English grammar reference book"
                    },
                    new LibraryBookEntity
                    {
                        Id = "book_003",
                        BookId = "BK003",
                        Title = "Things Fall Apart",
                        Author = "Chinua Achebe",
                        ISBN = "978-0-385-47454-2",
                        Category = "Literature",
                        Subcategory = "African Literature",
                        Publisher = "Anchor Books",
                        PublicationYear = 1994,
                        Language = "English",
                        TotalCopies = 40,
                        AvailableCopies = 35,
                        DamagedCopies = 2,
                        LostCopies = 3,
                        Description = "Classic African literature novel"
                    }
                };

                foreach (var book in books)
                {
                    await CreateOrUpdateBookAsync(book);
                }

                // Initialize book issues
                var bookIssues = new List<BookIssueEntity>
                {
                    new BookIssueEntity
                    {
                        Id = "issue_001",
                        BookId = "book_001",
                        StudentId = "student_001",
                        IssueDate = DateTime.UtcNow.AddDays(-10),
                        DueDate = DateTime.UtcNow.AddDays(4),
                        Status = "Issued",
                        IssuedBy = "Alice Nyambura",
                        Notes = "Mathematics textbook for Term 1"
                    },
                    new BookIssueEntity
                    {
                        Id = "issue_002",
                        BookId = "book_003",
                        StudentId = "student_002",
                        IssueDate = DateTime.UtcNow.AddDays(-5),
                        DueDate = DateTime.UtcNow.AddDays(9),
                        Status = "Issued",
                        IssuedBy = "Alice Nyambura",
                        Notes = "Literature study for English class"
                    }
                };

                foreach (var issue in bookIssues)
                {
                    await CreateOrUpdateBookIssueAsync(issue);
                }

                _logger?.LogInformation($"Initialized {books.Count} books and {bookIssues.Count} book issues");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing library books");
                throw;
            }
        }

        #endregion

        #region Assignments Initialization

        private async Task InitializeAssignmentsAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing assignments...");

                var assignments = new List<AssignmentEntity>
                {
                    new AssignmentEntity
                    {
                        Id = "assignment_001",
                        AssignmentId = "ASG001",
                        Title = "Algebra Basics",
                        Subject = "Mathematics",
                        Description = "Complete exercises 1-20 on algebraic expressions and simplification",
                        Instructions = "Show all working steps clearly. Submit handwritten solutions.",
                        DueDate = DateTime.UtcNow.AddDays(7),
                        MaxMarks = 100,
                        Form = "Form1",
                        TargetClasses = new List<string> { "Form1S", "Form1N" },
                        AssignmentType = "Assignment",
                        IsPublished = true,
                        AllowLateSubmission = true,
                        LatePenaltyPercentage = 10m,
                        CreatedBy = "teacher_001"
                    },
                    new AssignmentEntity
                    {
                        Id = "assignment_002",
                        AssignmentId = "ASG002",
                        Title = "Essay: My School Experience",
                        Subject = "English",
                        Description = "Write a 500-word essay about your school experience so far",
                        Instructions = "Use proper grammar, punctuation, and paragraph structure. Minimum 500 words.",
                        DueDate = DateTime.UtcNow.AddDays(10),
                        MaxMarks = 50,
                        Form = "Form2",
                        TargetClasses = new List<string> { "Form2N", "Form2C" },
                        AssignmentType = "Essay",
                        IsPublished = true,
                        AllowLateSubmission = false,
                        CreatedBy = "teacher_002"
                    }
                };

                foreach (var assignment in assignments)
                {
                    await CreateOrUpdateAssignmentAsync(assignment);
                }

                _logger?.LogInformation($"Initialized {assignments.Count} assignments");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing assignments");
                throw;
            }
        }

        #endregion

        #region Activities Initialization

        private async Task InitializeActivitiesAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing activities...");

                var activities = new List<ActivityEntity>
                {
                    new ActivityEntity
                    {
                        Id = "activity_001",
                        ActivityId = "ACT001",
                        Title = "Inter-House Sports Competition",
                        Description = "Annual sports competition between school houses",
                        Date = DateTime.UtcNow.AddDays(14),
                        StartTime = DateTime.UtcNow.AddDays(14).Date.AddHours(8),
                        EndTime = DateTime.UtcNow.AddDays(14).Date.AddHours(16),
                        Venue = "School Sports Field",
                        ActivityType = "Sports",
                        Organizer = "Sports Department",
                        TargetForms = new List<string> { "Form1", "Form2", "Form3", "Form4" },
                        IsOptional = false,
                        RegistrationDeadline = DateTime.UtcNow.AddDays(10),
                        MaxParticipants = 200,
                        CurrentParticipants = 45,
                        Status = "Active"
                    },
                    new ActivityEntity
                    {
                        Id = "activity_002",
                        ActivityId = "ACT002",
                        Title = "Science Fair",
                        Description = "Annual science project exhibition and competition",
                        Date = DateTime.UtcNow.AddDays(21),
                        StartTime = DateTime.UtcNow.AddDays(21).Date.AddHours(9),
                        EndTime = DateTime.UtcNow.AddDays(21).Date.AddHours(15),
                        Venue = "Science Laboratory",
                        ActivityType = "Academic",
                        Organizer = "Science Department",
                        TargetForms = new List<string> { "Form2", "Form3", "Form4" },
                        IsOptional = true,
                        RegistrationDeadline = DateTime.UtcNow.AddDays(18),
                        MaxParticipants = 50,
                        CurrentParticipants = 15,
                        Requirements = "Prepare a science project and presentation",
                        Status = "Active"
                    }
                };

                foreach (var activity in activities)
                {
                    await CreateOrUpdateActivityAsync(activity);
                }

                _logger?.LogInformation($"Initialized {activities.Count} activities");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing activities");
                throw;
            }
        }

        #endregion

        #region Notifications Initialization

        private async Task InitializeNotificationsAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing notifications...");

                var notifications = new List<NotificationEntity>
                {
                    new NotificationEntity
                    {
                        Id = "notif_001",
                        Title = "Assignment Due Reminder",
                        Message = "Your Mathematics assignment 'Algebra Basics' is due in 2 days.",
                        RecipientId = "user_001",
                        NotificationType = "Assignment",
                        Priority = "Medium",
                        ActionUrl = "/assignments/assignment_001",
                        ExpiryDate = DateTime.UtcNow.AddDays(3),
                        IsRead = false,
                        CreatedBy = "system"
                    },
                    new NotificationEntity
                    {
                        Id = "notif_002",
                        Title = "Fees Payment Reminder",
                        Message = "Your school fees balance is KES 55,000. Please make payment before due date.",
                        RecipientId = "user_001",
                        NotificationType = "Fees",
                        Priority = "High",
                        ActionUrl = "/fees",
                        ExpiryDate = DateTime.UtcNow.AddDays(7),
                        IsRead = false,
                        CreatedBy = "system"
                    },
                    new NotificationEntity
                    {
                        Id = "notif_003",
                        Title = "Book Return Reminder",
                        Message = "Please return 'Mathematics Form 1' book by the due date to avoid fines.",
                        RecipientId = "user_001",
                        NotificationType = "Library",
                        Priority = "Medium",
                        ActionUrl = "/library/my-books",
                        ExpiryDate = DateTime.UtcNow.AddDays(5),
                        IsRead = false,
                        CreatedBy = "system"
                    }
                };

                foreach (var notification in notifications)
                {
                    await CreateOrUpdateNotificationAsync(notification);
                }

                _logger?.LogInformation($"Initialized {notifications.Count} notifications");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing notifications");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Hash password using SHA256
        /// </summary>
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Create or update user in database
        /// </summary>
        private async Task CreateOrUpdateUserAsync(UserEntity user)
        {
            try
            {
                var existingUser = await _supabaseClient
                    .From<UserEntity>()
                    .Where(x => x.Id == user.Id)
                    .Single();

                if (existingUser == null)
                {
                    await _supabaseClient.From<UserEntity>().Insert(user);
                    _logger?.LogDebug("Created user: {UserId}", user.Id);
                }
                else
                {
                    // Update existing user
                    existingUser.PhoneNumber = user.PhoneNumber;
                    existingUser.UserType = user.UserType;
                    existingUser.IsActive = user.IsActive;
                    existingUser.ProfileCompleted = user.ProfileCompleted;
                    existingUser.TermsAccepted = user.TermsAccepted;
                    existingUser.UpdatedAt = DateTime.UtcNow;

                    await existingUser.Update<UserEntity>();
                    _logger?.LogDebug("Updated user: {UserId}", user.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating user: {UserId}", user.Id);
                throw;
            }
        }

        /// <summary>
        /// Create or update student in database
        /// </summary>
        private async Task CreateOrUpdateStudentAsync(StudentEntity student)
        {
            try
            {
                var existingStudent = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.Id == student.Id)
                    .Single();

                if (existingStudent == null)
                {
                    await _supabaseClient.From<StudentEntity>().Insert(student);
                    _logger?.LogDebug("Created student: {StudentId}", student.StudentId);
                }
                else
                {
                    // Update existing student
                    existingStudent.FullName = student.FullName;
                    existingStudent.Form = student.Form;
                    existingStudent.Class = student.Class;
                    existingStudent.Email = student.Email;
                    existingStudent.ParentPhone = student.ParentPhone;
                    existingStudent.GuardianName = student.GuardianName;
                    existingStudent.GuardianPhone = student.GuardianPhone;
                    existingStudent.Status = student.Status;
                    existingStudent.UpdatedAt = DateTime.UtcNow;

                    await existingStudent.Update<StudentEntity>();
                    _logger?.LogDebug("Updated student: {StudentId}", student.StudentId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating student: {StudentId}", student.StudentId);
                throw;
            }
        }

        /// <summary>
        /// Create or update teacher in database
        /// </summary>
        private async Task CreateOrUpdateTeacherAsync(TeacherEntity teacher)
        {
            try
            {
                var existingTeacher = await _supabaseClient
                    .From<TeacherEntity>()
                    .Where(x => x.Id == teacher.Id)
                    .Single();

                if (existingTeacher == null)
                {
                    await _supabaseClient.From<TeacherEntity>().Insert(teacher);
                    _logger?.LogDebug("Created teacher: {TeacherId}", teacher.TeacherId);
                }
                else
                {
                    // Update existing teacher
                    existingTeacher.FullName = teacher.FullName;
                    existingTeacher.EmployeeType = teacher.EmployeeType;
                    existingTeacher.TscNumber = teacher.TscNumber;
                    existingTeacher.Email = teacher.Email;
                    existingTeacher.PhoneNumber = teacher.PhoneNumber;
                    existingTeacher.Subjects = teacher.Subjects;
                    existingTeacher.QualifiedSubjects = teacher.QualifiedSubjects;
                    existingTeacher.AssignedForms = teacher.AssignedForms;
                    existingTeacher.Department = teacher.Department;
                    existingTeacher.IsClassTeacher = teacher.IsClassTeacher;
                    existingTeacher.ClassTeacherFor = teacher.ClassTeacherFor;
                    existingTeacher.UpdatedAt = DateTime.UtcNow;

                    await existingTeacher.Update<TeacherEntity>();
                    _logger?.LogDebug("Updated teacher: {TeacherId}", teacher.TeacherId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating teacher: {TeacherId}", teacher.TeacherId);
                throw;
            }
        }

        /// <summary>
        /// Create or update staff in database
        /// </summary>
        private async Task CreateOrUpdateStaffAsync(StaffEntity staff)
        {
            try
            {
                var existingStaff = await _supabaseClient
                    .From<StaffEntity>()
                    .Where(x => x.Id == staff.Id)
                    .Single();

                if (existingStaff == null)
                {
                    await _supabaseClient.From<StaffEntity>().Insert(staff);
                    _logger?.LogDebug("Created staff: {StaffId}", staff.StaffId);
                }
                else
                {
                    // Update existing staff
                    existingStaff.FullName = staff.FullName;
                    existingStaff.Position = staff.Position;
                    existingStaff.Department = staff.Department;
                    existingStaff.Email = staff.Email;
                    existingStaff.PhoneNumber = staff.PhoneNumber;
                    existingStaff.Qualification = staff.Qualification;
                    existingStaff.Salary = staff.Salary;
                    existingStaff.OfficeLocation = staff.OfficeLocation;
                    existingStaff.Permissions = staff.Permissions;
                    existingStaff.UpdatedAt = DateTime.UtcNow;

                    await existingStaff.Update<StaffEntity>();
                    _logger?.LogDebug("Updated staff: {StaffId}", staff.StaffId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating staff: {StaffId}", staff.StaffId);
                throw;
            }
        }

        /// <summary>
        /// Create or update fees in database
        /// </summary>
        private async Task CreateOrUpdateFeesAsync(FeesEntity fees)
        {
            try
            {
                var existingFees = await _supabaseClient
                    .From<FeesEntity>()
                    .Where(x => x.Id == fees.Id)
                    .Single();

                if (existingFees == null)
                {
                    await _supabaseClient.From<FeesEntity>().Insert(fees);
                    _logger?.LogDebug("Created fees record for student: {StudentId}", fees.StudentId);
                }
                else
                {
                    // Update existing fees
                    existingFees.TotalFees = fees.TotalFees;
                    existingFees.PaidAmount = fees.PaidAmount;
                    existingFees.Balance = fees.Balance;
                    existingFees.PaymentStatus = fees.PaymentStatus;
                    existingFees.LastPaymentDate = fees.LastPaymentDate;
                    existingFees.UpdatedAt = DateTime.UtcNow;

                    await existingFees.Update<FeesEntity>();
                    _logger?.LogDebug("Updated fees record for student: {StudentId}", fees.StudentId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating fees for student: {StudentId}", fees.StudentId);
                throw;
            }
        }

        /// <summary>
        /// Create or update payment in database
        /// </summary>
        private async Task CreateOrUpdatePaymentAsync(FeesPaymentEntity payment)
        {
            try
            {
                var existingPayment = await _supabaseClient
                    .From<FeesPaymentEntity>()
                    .Where(x => x.Id == payment.Id)
                    .Single();

                if (existingPayment == null)
                {
                    await _supabaseClient.From<FeesPaymentEntity>().Insert(payment);
                    _logger?.LogDebug("Created payment record: {PaymentId}", payment.Id);
                }
                else
                {
                    // Update existing payment
                    existingPayment.Amount = payment.Amount;
                    existingPayment.PaymentMethod = payment.PaymentMethod;
                    existingPayment.ReceiptNumber = payment.ReceiptNumber;
                    existingPayment.TransactionId = payment.TransactionId;
                    existingPayment.IsApproved = payment.IsApproved;
                    existingPayment.Description = payment.Description;
                    existingPayment.UpdatedAt = DateTime.UtcNow;

                    await existingPayment.Update<FeesPaymentEntity>();
                    _logger?.LogDebug("Updated payment record: {PaymentId}", payment.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating payment: {PaymentId}", payment.Id);
                throw;
            }
        }

        /// <summary>
        /// Create or update book in database
        /// </summary>
        private async Task CreateOrUpdateBookAsync(LibraryBookEntity book)
        {
            try
            {
                var existingBook = await _supabaseClient
                    .From<LibraryBookEntity>()
                    .Where(x => x.Id == book.Id)
                    .Single();

                if (existingBook == null)
                {
                    await _supabaseClient.From<LibraryBookEntity>().Insert(book);
                    _logger?.LogDebug("Created book: {BookId}", book.BookId);
                }
                else
                {
                    // Update existing book
                    existingBook.Title = book.Title;
                    existingBook.Author = book.Author;
                    existingBook.ISBN = book.ISBN;
                    existingBook.Category = book.Category;
                    existingBook.Publisher = book.Publisher;
                    existingBook.TotalCopies = book.TotalCopies;
                    existingBook.AvailableCopies = book.AvailableCopies;
                    existingBook.DamagedCopies = book.DamagedCopies;
                    existingBook.LostCopies = book.LostCopies;
                    existingBook.UpdatedAt = DateTime.UtcNow;

                    await existingBook.Update<LibraryBookEntity>();
                    _logger?.LogDebug("Updated book: {BookId}", book.BookId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating book: {BookId}", book.BookId);
                throw;
            }
        }

        /// <summary>
        /// Create or update book issue in database
        /// </summary>
        private async Task CreateOrUpdateBookIssueAsync(BookIssueEntity issue)
        {
            try
            {
                var existingIssue = await _supabaseClient
                    .From<BookIssueEntity>()
                    .Where(x => x.Id == issue.Id)
                    .Single();

                if (existingIssue == null)
                {
                    await _supabaseClient.From<BookIssueEntity>().Insert(issue);
                    _logger?.LogDebug("Created book issue: {IssueId}", issue.Id);
                }
                else
                {
                    // Update existing issue
                    existingIssue.DueDate = issue.DueDate;
                    existingIssue.ReturnDate = issue.ReturnDate;
                    existingIssue.Status = issue.Status;
                    existingIssue.FineAmount = issue.FineAmount;
                    existingIssue.FinePaid = issue.FinePaid;
                    existingIssue.Notes = issue.Notes;
                    existingIssue.UpdatedAt = DateTime.UtcNow;

                    await existingIssue.Update<BookIssueEntity>();
                    _logger?.LogDebug("Updated book issue: {IssueId}", issue.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating book issue: {IssueId}", issue.Id);
                throw;
            }
        }

        /// <summary>
        /// Create or update assignment in database
        /// </summary>
        private async Task CreateOrUpdateAssignmentAsync(AssignmentEntity assignment)
        {
            try
            {
                var existingAssignment = await _supabaseClient
                    .From<AssignmentEntity>()
                    .Where(x => x.Id == assignment.Id)
                    .Single();

                if (existingAssignment == null)
                {
                    await _supabaseClient.From<AssignmentEntity>().Insert(assignment);
                    _logger?.LogDebug("Created assignment: {AssignmentId}", assignment.AssignmentId);
                }
                else
                {
                    // Update existing assignment
                    existingAssignment.Title = assignment.Title;
                    existingAssignment.Subject = assignment.Subject;
                    existingAssignment.Description = assignment.Description;
                    existingAssignment.Instructions = assignment.Instructions;
                    existingAssignment.DueDate = assignment.DueDate;
                    existingAssignment.MaxMarks = assignment.MaxMarks;
                    existingAssignment.Form = assignment.Form;
                    existingAssignment.TargetClasses = assignment.TargetClasses;
                    existingAssignment.IsPublished = assignment.IsPublished;
                    existingAssignment.AllowLateSubmission = assignment.AllowLateSubmission;
                    existingAssignment.LatePenaltyPercentage = assignment.LatePenaltyPercentage;
                    existingAssignment.UpdatedAt = DateTime.UtcNow;

                    await existingAssignment.Update<AssignmentEntity>();
                    _logger?.LogDebug("Updated assignment: {AssignmentId}", assignment.AssignmentId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating assignment: {AssignmentId}", assignment.AssignmentId);
                throw;
            }
        }

        /// <summary>
        /// Create or update activity in database
        /// </summary>
        private async Task CreateOrUpdateActivityAsync(ActivityEntity activity)
        {
            try
            {
                var existingActivity = await _supabaseClient
                    .From<ActivityEntity>()
                    .Where(x => x.Id == activity.Id)
                    .Single();

                if (existingActivity == null)
                {
                    await _supabaseClient.From<ActivityEntity>().Insert(activity);
                    _logger?.LogDebug("Created activity: {ActivityId}", activity.ActivityId);
                }
                else
                {
                    // Update existing activity
                    existingActivity.Title = activity.Title;
                    existingActivity.Description = activity.Description;
                    existingActivity.Date = activity.Date;
                    existingActivity.StartTime = activity.StartTime;
                    existingActivity.EndTime = activity.EndTime;
                    existingActivity.Venue = activity.Venue;
                    existingActivity.ActivityType = activity.ActivityType;
                    existingActivity.Organizer = activity.Organizer;
                    existingActivity.TargetForms = activity.TargetForms;
                    existingActivity.IsOptional = activity.IsOptional;
                    existingActivity.RegistrationDeadline = activity.RegistrationDeadline;
                    existingActivity.MaxParticipants = activity.MaxParticipants;
                    existingActivity.CurrentParticipants = activity.CurrentParticipants;
                    existingActivity.Requirements = activity.Requirements;
                    existingActivity.Status = activity.Status;
                    existingActivity.UpdatedAt = DateTime.UtcNow;

                    await existingActivity.Update<ActivityEntity>();
                    _logger?.LogDebug("Updated activity: {ActivityId}", activity.ActivityId);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating activity: {ActivityId}", activity.ActivityId);
                throw;
            }
        }

        /// <summary>
        /// Create or update notification in database
        /// </summary>
        private async Task CreateOrUpdateNotificationAsync(NotificationEntity notification)
        {
            try
            {
                var existingNotification = await _supabaseClient
                    .From<NotificationEntity>()
                    .Where(x => x.Id == notification.Id)
                    .Single();

                if (existingNotification == null)
                {
                    await _supabaseClient.From<NotificationEntity>().Insert(notification);
                    _logger?.LogDebug("Created notification: {NotificationId}", notification.Id);
                }
                else
                {
                    // Update existing notification
                    existingNotification.Title = notification.Title;
                    existingNotification.Message = notification.Message;
                    existingNotification.NotificationType = notification.NotificationType;
                    existingNotification.Priority = notification.Priority;
                    existingNotification.ActionUrl = notification.ActionUrl;
                    existingNotification.ExpiryDate = notification.ExpiryDate;
                    existingNotification.IsRead = notification.IsRead;
                    existingNotification.UpdatedAt = DateTime.UtcNow;

                    await existingNotification.Update<NotificationEntity>();
                    _logger?.LogDebug("Updated notification: {NotificationId}", notification.Id);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating/updating notification: {NotificationId}", notification.Id);
                throw;
            }
        }

        #endregion
    }

    #region Notification Entity

    /// <summary>
    /// Notification entity for database operations
    /// </summary>
    [Supabase.Postgrest.Attributes.Table("notifications")]
    public class NotificationEntity : BaseSupabaseModel
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
    }

    #endregion
}