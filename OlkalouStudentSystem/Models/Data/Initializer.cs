// ===============================
// Data/Initializer.cs - Enhanced Database Setup and Migration
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models.Data;
using OlkalouStudentSystem.Services;
using System.Diagnostics;
using System.Text.Json;

namespace OlkalouStudentSystem.Data
{
    /// <summary>
    /// Enhanced database initializer with comprehensive error handling and migration capabilities
    /// </summary>
    public sealed class DatabaseInitializer : IDisposable
    {
        #region Fields and Constants

        private readonly SupabaseService _supabaseService;
        private readonly ILogger<DatabaseInitializer>? _logger;
        private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
        private bool _disposed = false;

        // Preference keys for tracking initialization state
        private const string DatabaseTablesCreatedKey = "supabase_tables_verified";
        private const string DummyDataMigratedKey = "dummy_data_migrated_v2";
        private const string GradingSystemMigratedKey = "grading_system_migrated";
        private const string DefaultUsersCreatedKey = "default_users_created";
        private const string LastMigrationVersionKey = "last_migration_version";

        // Current migration version
        private const int CurrentMigrationVersion = 2;

        // Retry configuration
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMs = 1000;

        #endregion

        #region Constructor

        public DatabaseInitializer(SupabaseService? supabaseService = null, ILogger<DatabaseInitializer>? logger = null)
        {
            _supabaseService = supabaseService ?? SupabaseService.Instance;
            _logger = logger;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize database with comprehensive error handling and progress tracking
        /// </summary>
        public async Task<InitializationResult> InitializeAsync(IProgress<InitializationProgress>? progress = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DatabaseInitializer));

            await _initializationSemaphore.WaitAsync();

            try
            {
                _logger?.LogInformation("Starting database initialization...");
                var stopwatch = Stopwatch.StartNew();
                var result = new InitializationResult();

                // Step 1: Verify Supabase connection
                progress?.Report(new InitializationProgress { Step = "Verifying Connection", Percentage = 10 });
                var connectionResult = await VerifyConnectionAsync();
                if (!connectionResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = connectionResult.ErrorMessage;
                    return result;
                }

                // Step 2: Check and create tables if needed
                progress?.Report(new InitializationProgress { Step = "Verifying Tables", Percentage = 20 });
                var tablesResult = await VerifyAndCreateTablesAsync();
                if (!tablesResult.Success)
                {
                    result.Success = false;
                    result.ErrorMessage = tablesResult.ErrorMessage;
                    return result;
                }

                // Step 3: Migrate grading system
                progress?.Report(new InitializationProgress { Step = "Setting up Grading System", Percentage = 40 });
                var gradingResult = await MigrateGradingSystemAsync();
                if (!gradingResult.Success)
                {
                    _logger?.LogWarning("Grading system migration failed: {Error}", gradingResult.ErrorMessage);
                    // Don't fail initialization for grading system issues
                }

                // Step 4: Create default users
                progress?.Report(new InitializationProgress { Step = "Creating Default Users", Percentage = 60 });
                var usersResult = await CreateDefaultUsersAsync();
                if (!usersResult.Success)
                {
                    _logger?.LogWarning("Default users creation failed: {Error}", usersResult.ErrorMessage);
                    // Don't fail initialization for default users issues
                }

                // Step 5: Migrate sample data
                progress?.Report(new InitializationProgress { Step = "Setting up Sample Data", Percentage = 80 });
                var sampleDataResult = await MigrateSampleDataAsync();
                if (!sampleDataResult.Success)
                {
                    _logger?.LogWarning("Sample data migration failed: {Error}", sampleDataResult.ErrorMessage);
                    // Don't fail initialization for sample data issues
                }

                // Step 6: Update migration version
                progress?.Report(new InitializationProgress { Step = "Finalizing", Percentage = 100 });
                Preferences.Set(LastMigrationVersionKey, CurrentMigrationVersion);

                stopwatch.Stop();
                result.Success = true;
                result.Duration = stopwatch.Elapsed;

                _logger?.LogInformation("Database initialization completed successfully in {Duration}ms",
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Database initialization failed with unexpected error");
                return new InitializationResult
                {
                    Success = false,
                    ErrorMessage = $"Unexpected error during initialization: {ex.Message}"
                };
            }
            finally
            {
                _initializationSemaphore.Release();
            }
        }

        /// <summary>
        /// Reset database state (for testing/development purposes)
        /// </summary>
        public async Task<OperationResult> ResetDatabaseAsync(bool includeUserData = false)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DatabaseInitializer));

            try
            {
                _logger?.LogWarning("Resetting database state...");

                // Clear preferences
                var preferencesToClear = new[]
                {
                    DatabaseTablesCreatedKey,
                    DummyDataMigratedKey,
                    GradingSystemMigratedKey,
                    DefaultUsersCreatedKey,
                    LastMigrationVersionKey
                };

                foreach (var key in preferencesToClear)
                {
                    if (Preferences.ContainsKey(key))
                        Preferences.Remove(key);
                }

                if (includeUserData)
                {
                    // Note: In production, you would need proper data deletion methods
                    // This is a simplified version for development/testing
                    _logger?.LogWarning("User data reset requested but not implemented for safety");
                }

                _logger?.LogInformation("Database state reset completed");
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Database reset failed");
                return OperationResult.Failure($"Database reset failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get initialization status
        /// </summary>
        public InitializationStatus GetInitializationStatus()
        {
            return new InitializationStatus
            {
                TablesVerified = Preferences.Get(DatabaseTablesCreatedKey, false),
                GradingSystemMigrated = Preferences.Get(GradingSystemMigratedKey, false),
                DefaultUsersCreated = Preferences.Get(DefaultUsersCreatedKey, false),
                SampleDataMigrated = Preferences.Get(DummyDataMigratedKey, false),
                LastMigrationVersion = Preferences.Get(LastMigrationVersionKey, 0),
                CurrentMigrationVersion = CurrentMigrationVersion,
                IsUpToDate = Preferences.Get(LastMigrationVersionKey, 0) >= CurrentMigrationVersion
            };
        }

        #endregion

        #region Private Methods - Connection and Table Verification

        /// <summary>
        /// Verify Supabase connection
        /// </summary>
        private async Task<OperationResult> VerifyConnectionAsync()
        {
            try
            {
                if (!_supabaseService.IsInitialized)
                {
                    await _supabaseService.InitializeAsync();
                }

                var healthCheck = await _supabaseService.HealthCheckAsync();
                if (!healthCheck.IsHealthy)
                {
                    return OperationResult.Failure($"Supabase connection unhealthy: {healthCheck.Message}");
                }

                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Connection verification failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Verify and create database tables if needed
        /// </summary>
        private async Task<OperationResult> VerifyAndCreateTablesAsync()
        {
            try
            {
                var tablesVerified = Preferences.Get(DatabaseTablesCreatedKey, false);
                if (tablesVerified)
                {
                    _logger?.LogDebug("Tables already verified, skipping verification");
                    return OperationResult.Success();
                }

                _logger?.LogInformation("Verifying database tables...");

                var tableVerificationTasks = new[]
                {
                    VerifyTableExistsAsync<UserEntity>("users"),
                    VerifyTableExistsAsync<StudentEntity>("students"),
                    VerifyTableExistsAsync<TeacherEntity>("teachers"),
                    VerifyTableExistsAsync<StaffEntity>("staff"),
                    VerifyTableExistsAsync<FeesEntity>("fees"),
                    VerifyTableExistsAsync<FeesPaymentEntity>("fees_payments"),
                    VerifyTableExistsAsync<AssignmentEntity>("assignments"),
                    VerifyTableExistsAsync<AssignmentSubmissionEntity>("assignment_submissions"),
                    VerifyTableExistsAsync<MarkEntity>("marks"),
                    VerifyTableExistsAsync<GradingSystemEntity>("grading_system"),
                    VerifyTableExistsAsync<DisciplinaryIssueEntity>("disciplinary_issues"),
                    VerifyTableExistsAsync<LibraryBookEntity>("library_books"),
                    VerifyTableExistsAsync<BookIssueEntity>("book_issues"),
                    VerifyTableExistsAsync<ActivityEntity>("activities"),
                    VerifyTableExistsAsync<ActivityRegistrationEntity>("activity_registrations"),
                    VerifyTableExistsAsync<AchievementEntity>("achievements"),
                    VerifyTableExistsAsync<AnnouncementEntity>("announcements"),
                    VerifyTableExistsAsync<AttendanceEntity>("attendance"),
                    VerifyTableExistsAsync<TimetableEntity>("timetable"),
                    VerifyTableExistsAsync<ExamEntity>("exams"),
                    VerifyTableExistsAsync<AppSettingsEntity>("app_settings"),
                    VerifyTableExistsAsync<AuditLogEntity>("audit_logs")
                };

                var results = await Task.WhenAll(tableVerificationTasks);
                var failedTables = results.Where(r => !r.Success).ToList();

                if (failedTables.Any())
                {
                    var errorMessage = $"Table verification failed for: {string.Join(", ", failedTables.Select(r => r.ErrorMessage))}";
                    return OperationResult.Failure(errorMessage);
                }

                Preferences.Set(DatabaseTablesCreatedKey, true);
                _logger?.LogInformation("All database tables verified successfully");
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Table verification failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Verify a specific table exists
        /// </summary>
        private async Task<OperationResult> VerifyTableExistsAsync<T>(string tableName) where T : BaseSupabaseModel, new()
        {
            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                try
                {
                    var result = await _supabaseService.Client.From<T>().Limit(1).Get();
                    _logger?.LogDebug("Table '{TableName}' verified successfully", tableName);
                    return OperationResult.Success();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning("Table verification attempt {Attempt}/{MaxAttempts} failed for '{TableName}': {Error}",
                        attempt, MaxRetryAttempts, tableName, ex.Message);

                    if (attempt == MaxRetryAttempts)
                    {
                        return OperationResult.Failure($"Table '{tableName}' verification failed: {ex.Message}");
                    }

                    await Task.Delay(RetryDelayMs * attempt);
                }
            }

            return OperationResult.Failure($"Table '{tableName}' verification failed after {MaxRetryAttempts} attempts");
        }

        #endregion

        #region Private Methods - Data Migration

        /// <summary>
        /// Migrate grading system data
        /// </summary>
        private async Task<OperationResult> MigrateGradingSystemAsync()
        {
            try
            {
                var gradingMigrated = Preferences.Get(GradingSystemMigratedKey, false);
                if (gradingMigrated)
                {
                    _logger?.LogDebug("Grading system already migrated, skipping");
                    return OperationResult.Success();
                }

                _logger?.LogInformation("Migrating grading system...");

                // Check if grading system already exists
                var existingGrades = await _supabaseService.Client.From<GradingSystemEntity>().Get();
                if (existingGrades?.Models?.Count > 0)
                {
                    _logger?.LogInformation("Grading system already exists, marking as migrated");
                    Preferences.Set(GradingSystemMigratedKey, true);
                    return OperationResult.Success();
                }

                var gradingSystem = CreateKenyanGradingSystem();

                foreach (var grade in gradingSystem)
                {
                    try
                    {
                        grade.ValidateAndThrow();
                        await _supabaseService.Client.From<GradingSystemEntity>().Insert(grade);
                        await Task.Delay(50); // Small delay to prevent rate limiting
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to insert grade {Grade}", grade.Grade);
                        throw;
                    }
                }

                Preferences.Set(GradingSystemMigratedKey, true);
                _logger?.LogInformation("Grading system migrated successfully with {Count} grades", gradingSystem.Count);
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Grading system migration failed");
                return OperationResult.Failure($"Grading system migration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Create default users and their profiles
        /// </summary>
        private async Task<OperationResult> CreateDefaultUsersAsync()
        {
            try
            {
                var usersCreated = Preferences.Get(DefaultUsersCreatedKey, false);
                if (usersCreated)
                {
                    _logger?.LogDebug("Default users already created, skipping");
                    return OperationResult.Success();
                }

                _logger?.LogInformation("Creating default users...");

                // Check if users already exist
                var existingUsers = await _supabaseService.Client.From<UserEntity>().Get();
                if (existingUsers?.Models?.Count > 0)
                {
                    _logger?.LogInformation("Users already exist, marking as created");
                    Preferences.Set(DefaultUsersCreatedKey, true);
                    return OperationResult.Success();
                }

                var defaultUsers = CreateDefaultUserProfiles();

                foreach (var (userEntity, profileEntity) in defaultUsers)
                {
                    try
                    {
                        // Validate entities before insertion
                        userEntity.ValidateAndThrow();

                        // Insert user first
                        await _supabaseService.Client.From<UserEntity>().Insert(userEntity);
                        _logger?.LogDebug("Created user: {UserType} - {Phone}", userEntity.UserType, userEntity.PhoneNumber);

                        // Insert corresponding profile
                        await InsertUserProfileAsync(userEntity.Id, profileEntity);

                        await Task.Delay(100); // Small delay between users
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Failed to create user {UserType} - {Phone}",
                            userEntity.UserType, userEntity.PhoneNumber);
                        throw;
                    }
                }

                Preferences.Set(DefaultUsersCreatedKey, true);
                _logger?.LogInformation("Default users created successfully: {Count} users", defaultUsers.Count);
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Default users creation failed");
                return OperationResult.Failure($"Default users creation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Insert user profile based on type
        /// </summary>
        private async Task InsertUserProfileAsync(string userId, object profileEntity)
        {
            switch (profileEntity)
            {
                case StaffEntity staff:
                    staff.UserId = userId;
                    staff.ValidateAndThrow();
                    await _supabaseService.Client.From<StaffEntity>().Insert(staff);
                    break;

                case TeacherEntity teacher:
                    teacher.UserId = userId;
                    teacher.ValidateAndThrow();
                    await _supabaseService.Client.From<TeacherEntity>().Insert(teacher);
                    break;

                case StudentEntity student:
                    student.UserId = userId;
                    student.ValidateAndThrow();
                    await _supabaseService.Client.From<StudentEntity>().Insert(student);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown profile entity type: {profileEntity.GetType()}");
            }
        }

        /// <summary>
        /// Migrate sample data for testing and demonstration
        /// </summary>
        private async Task<OperationResult> MigrateSampleDataAsync()
        {
            try
            {
                var sampleDataMigrated = Preferences.Get(DummyDataMigratedKey, false);
                if (sampleDataMigrated)
                {
                    _logger?.LogDebug("Sample data already migrated, skipping");
                    return OperationResult.Success();
                }

                _logger?.LogInformation("Migrating sample data...");

                var tasks = new[]
                {
                    MigrateSampleLibraryBooksAsync(),
                    MigrateSampleActivitiesAsync(),
                    MigrateSampleFeesAsync(),
                    MigrateSampleAnnouncementsAsync()
                };

                var results = await Task.WhenAll(tasks);
                var failures = results.Where(r => !r.Success).ToList();

                if (failures.Any())
                {
                    var errorMessage = $"Some sample data migration failed: {string.Join("; ", failures.Select(f => f.ErrorMessage))}";
                    _logger?.LogWarning(errorMessage);
                    // Don't fail overall migration for sample data issues
                }

                Preferences.Set(DummyDataMigratedKey, true);
                _logger?.LogInformation("Sample data migration completed");
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Sample data migration failed");
                return OperationResult.Failure($"Sample data migration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Migrate sample library books
        /// </summary>
        private async Task<OperationResult> MigrateSampleLibraryBooksAsync()
        {
            try
            {
                var existingBooks = await _supabaseService.Client.From<LibraryBookEntity>().Limit(1).Get();
                if (existingBooks?.Models?.Count > 0)
                {
                    return OperationResult.Success(); // Books already exist
                }

                var sampleBooks = CreateSampleLibraryBooks();

                foreach (var book in sampleBooks)
                {
                    book.ValidateAndThrow();
                    await _supabaseService.Client.From<LibraryBookEntity>().Insert(book);
                    await Task.Delay(50);
                }

                _logger?.LogDebug("Sample library books migrated: {Count} books", sampleBooks.Count);
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Library books migration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Migrate sample activities
        /// </summary>
        private async Task<OperationResult> MigrateSampleActivitiesAsync()
        {
            try
            {
                var existingActivities = await _supabaseService.Client.From<ActivityEntity>().Limit(1).Get();
                if (existingActivities?.Models?.Count > 0)
                {
                    return OperationResult.Success(); // Activities already exist
                }

                var sampleActivities = CreateSampleActivities();

                foreach (var activity in sampleActivities)
                {
                    activity.ValidateAndThrow();
                    await _supabaseService.Client.From<ActivityEntity>().Insert(activity);
                    await Task.Delay(50);
                }

                _logger?.LogDebug("Sample activities migrated: {Count} activities", sampleActivities.Count);
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Activities migration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Migrate sample fees data
        /// </summary>
        private async Task<OperationResult> MigrateSampleFeesAsync()
        {
            try
            {
                // Get a sample student first
                var students = await _supabaseService.Client.From<StudentEntity>().Limit(1).Get();
                if (students?.Models?.Count == 0)
                {
                    return OperationResult.Success(); // No students to create fees for
                }

                var student = students.Models.First();

                // Check if fees already exist for this student
                var existingFees = await _supabaseService.Client
                    .From<FeesEntity>()
                    .Where(x => x.StudentId == student.Id)
                    .Get();

                if (existingFees?.Models?.Count > 0)
                {
                    return OperationResult.Success(); // Fees already exist
                }

                var fees = CreateSampleFeesForStudent(student.Id);
                fees.ValidateAndThrow();
                await _supabaseService.Client.From<FeesEntity>().Insert(fees);

                _logger?.LogDebug("Sample fees migrated for student {StudentId}", student.StudentId);
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Fees migration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Migrate sample announcements
        /// </summary>
        private async Task<OperationResult> MigrateSampleAnnouncementsAsync()
        {
            try
            {
                var existingAnnouncements = await _supabaseService.Client.From<AnnouncementEntity>().Limit(1).Get();
                if (existingAnnouncements?.Models?.Count > 0)
                {
                    return OperationResult.Success(); // Announcements already exist
                }

                var sampleAnnouncements = CreateSampleAnnouncements();

                foreach (var announcement in sampleAnnouncements)
                {
                    announcement.ValidateAndThrow();
                    await _supabaseService.Client.From<AnnouncementEntity>().Insert(announcement);
                    await Task.Delay(50);
                }

                _logger?.LogDebug("Sample announcements migrated: {Count} announcements", sampleAnnouncements.Count);
                return OperationResult.Success();
            }
            catch (Exception ex)
            {
                return OperationResult.Failure($"Announcements migration failed: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods - Data Creation

        /// <summary>
        /// Create Kenyan grading system
        /// </summary>
        private static List<GradingSystemEntity> CreateKenyanGradingSystem()
        {
            return new List<GradingSystemEntity>
            {
                new() { Id = Guid.NewGuid().ToString(), Grade = "A", MinPercentage = 80, MaxPercentage = 100, Points = 12, Description = "Excellent", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "A-", MinPercentage = 75, MaxPercentage = 79, Points = 11, Description = "Very Good", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "B+", MinPercentage = 70, MaxPercentage = 74, Points = 10, Description = "Good", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "B", MinPercentage = 65, MaxPercentage = 69, Points = 9, Description = "Good", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "B-", MinPercentage = 60, MaxPercentage = 64, Points = 8, Description = "Above Average", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "C+", MinPercentage = 55, MaxPercentage = 59, Points = 7, Description = "Above Average", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "C", MinPercentage = 50, MaxPercentage = 54, Points = 6, Description = "Average", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "C-", MinPercentage = 45, MaxPercentage = 49, Points = 5, Description = "Average", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "D+", MinPercentage = 40, MaxPercentage = 44, Points = 4, Description = "Below Average", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "D", MinPercentage = 35, MaxPercentage = 39, Points = 3, Description = "Below Average", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "D-", MinPercentage = 30, MaxPercentage = 34, Points = 2, Description = "Poor", CreatedBy = "system" },
                new() { Id = Guid.NewGuid().ToString(), Grade = "E", MinPercentage = 0, MaxPercentage = 29, Points = 1, Description = "Very Poor", CreatedBy = "system" }
            };
        }

        /// <summary>
        /// Create default user profiles for the system
        /// </summary>
        private static List<(UserEntity user, object profile)> CreateDefaultUserProfiles()
        {
            return new List<(UserEntity user, object profile)>
            {
                // Principal
                (new UserEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    PhoneNumber = "254724437239".FormatKenyanPhone(),
                    Password = BCrypt.Net.BCrypt.HashPassword("12345"),
                    UserType = "Principal",
                    Email = "principal@graceschool.ac.ke",
                    ProfileCompleted = true,
                    TermsAccepted = true,
                    TermsAcceptedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new StaffEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StaffId = "PRC/2025/001",
                    FullName = "Dr. Grace Wanjiku",
                    Position = "Principal",
                    Department = "Administration",
                    Email = "principal@graceschool.ac.ke",
                    PhoneNumber = "254724437239".FormatKenyanPhone(),
                    EmploymentDate = DateTime.UtcNow.AddYears(-5),
                    Salary = 150000,
                    Permissions = new List<string> { "ALL" },
                    CreatedBy = "system"
                }),

                // Deputy Principal
                (new UserEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    PhoneNumber = "254700998877".FormatKenyanPhone(),
                    Password = BCrypt.Net.BCrypt.HashPassword("12345"),
                    UserType = "DeputyPrincipal",
                    Email = "deputy@graceschool.ac.ke",
                    ProfileCompleted = true,
                    TermsAccepted = true,
                    TermsAcceptedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new StaffEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StaffId = "DPR/2025/001",
                    FullName = "Mr. John Kamau",
                    Position = "DeputyPrincipal",
                    Department = "Academic Affairs",
                    Email = "deputy@graceschool.ac.ke",
                    PhoneNumber = "254700998877".FormatKenyanPhone(),
                    EmploymentDate = DateTime.UtcNow.AddYears(-3),
                    Salary = 120000,
                    Permissions = new List<string> { "ACADEMIC", "DISCIPLINE", "REPORTS" },
                    CreatedBy = "system"
                }),

                // Secretary
                (new UserEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    PhoneNumber = "254712345678".FormatKenyanPhone(),
                    Password = BCrypt.Net.BCrypt.HashPassword("12345"),
                    UserType = "Secretary",
                    Email = "secretary@graceschool.ac.ke",
                    ProfileCompleted = true,
                    TermsAccepted = true,
                    TermsAcceptedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new StaffEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StaffId = "SEC/2025/001",
                    FullName = "Mrs. Mary Nyambura",
                    Position = "Secretary",
                    Department = "Administration",
                    Email = "secretary@graceschool.ac.ke",
                    PhoneNumber = "254712345678".FormatKenyanPhone(),
                    EmploymentDate = DateTime.UtcNow.AddYears(-2),
                    Salary = 60000,
                    Permissions = new List<string> { "STUDENTS", "COMMUNICATION", "RECORDS" },
                    CreatedBy = "system"
                }),

                // Sample Teacher
                (new UserEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    PhoneNumber = "254711223344".FormatKenyanPhone(),
                    Password = BCrypt.Net.BCrypt.HashPassword("12345"),
                    UserType = "Teacher",
                    Email = "teacher@graceschool.ac.ke",
                    ProfileCompleted = true,
                    TermsAccepted = true,
                    TermsAcceptedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new TeacherEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    TeacherId = "TCH/2025/001",
                    FullName = "Mr. Peter Mwangi",
                    EmployeeType = "NTSC",
                    TscNumber = "12345678",
                    Email = "teacher@graceschool.ac.ke",
                    PhoneNumber = "254711223344".FormatKenyanPhone(),
                    Subjects = new List<string> { "Mathematics", "Physics" },
                    AssignedForms = new List<string> { "Form3", "Form4" },
                    Department = "Sciences",
                    EmploymentDate = DateTime.UtcNow.AddYears(-1),
                    Qualification = "BSc. Mathematics, PGDE",
                    ExperienceYears = 5,
                    MonthlySalary = 45000,
                    IsClassTeacher = true,
                    ClassTeacherFor = "Form3A",
                    CreatedBy = "system"
                }),

                // Sample Student
                (new UserEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    PhoneNumber = "254700112233".FormatKenyanPhone(),
                    Password = BCrypt.Net.BCrypt.HashPassword("GRS/2025/001"),
                    UserType = "Student",
                    Email = "john.doe@graceschool.ac.ke",
                    ProfileCompleted = true,
                    TermsAccepted = true,
                    TermsAcceptedAt = DateTime.UtcNow,
                    CreatedBy = "system"
                },
                new StudentEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = "GRS/2025/001",
                    AdmissionNo = "GRS/001/2025",
                    FullName = "John Doe Mwangi",
                    Form = "Form4",
                    Class = "A",
                    Email = "john.doe@graceschool.ac.ke",
                    ParentPhone = "254700112233".FormatKenyanPhone(),
                    DateOfBirth = new DateTime(2006, 5, 15),
                    Gender = "Male",
                    Address = "P.O. Box 123, Ol Kalou",
                    GuardianName = "Jane Doe Mwangi",
                    GuardianPhone = "254700112234".FormatKenyanPhone(),
                    Year = DateTime.UtcNow.Year,
                    Term = GetCurrentTerm(),
                    EnrollmentDate = DateTime.UtcNow.AddYears(-3),
                    Status = "Active",
                    Nationality = "Kenyan",
                    Religion = "Christian",
                    CreatedBy = "system"
                })
            };
        }

        /// <summary>
        /// Create sample library books
        /// </summary>
        private static List<LibraryBookEntity> CreateSampleLibraryBooks()
        {
            return new List<LibraryBookEntity>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    BookId = "LIB001",
                    Title = "Advanced Mathematics Form 4",
                    Author = "Dr. James Wachira",
                    ISBN = "9789966000019",
                    Category = "Mathematics",
                    Subcategory = "Secondary Education",
                    Publisher = "East African Publishers",
                    PublicationYear = 2022,
                    Edition = "3rd Edition",
                    Language = "English",
                    TotalCopies = 25,
                    AvailableCopies = 18,
                    DamagedCopies = 2,
                    LostCopies = 1,
                    Description = "Comprehensive mathematics textbook covering Form 4 curriculum",
                    CreatedBy = "system"
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    BookId = "LIB002",
                    Title = "Physics Principles and Practice",
                    Author = "Prof. Mary Kiprotich",
                    ISBN = "9789966000026",
                    Category = "Science",
                    Subcategory = "Physics",
                    Publisher = "Longhorn Publishers",
                    PublicationYear = 2023,
                    Edition = "2nd Edition",
                    Language = "English",
                    TotalCopies = 20,
                    AvailableCopies = 15,
                    DamagedCopies = 1,
                    LostCopies = 0,
                    Description = "Detailed physics concepts for secondary school students",
                    CreatedBy = "system"
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    BookId = "LIB003",
                    Title = "English Literature Anthology",
                    Author = "Prof. Susan Macharia",
                    ISBN = "9789966000033",
                    Category = "Literature",
                    Subcategory = "English",
                    Publisher = "Kenya Literature Bureau",
                    PublicationYear = 2022,
                    Edition = "1st Edition",
                    Language = "English",
                    TotalCopies = 30,
                    AvailableCopies = 22,
                    DamagedCopies = 3,
                    LostCopies = 1,
                    Description = "Collection of African and world literature for secondary schools",
                    CreatedBy = "system"
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    BookId = "LIB004",
                    Title = "Kiswahili Fasihi",
                    Author = "Dkt. Ali Hassan",
                    ISBN = "9789966000040",
                    Category = "Languages",
                    Subcategory = "Kiswahili",
                    Publisher = "Jomo Kenyatta Foundation",
                    PublicationYear = 2021,
                    Edition = "4th Edition",
                    Language = "Kiswahili",
                    TotalCopies = 25,
                    AvailableCopies = 20,
                    DamagedCopies = 1,
                    LostCopies = 0,
                    Description = "Kitabu cha fasihi ya Kiswahili kwa shule za upili",
                    CreatedBy = "system"
                }
            };
        }

        /// <summary>
        /// Create sample activities
        /// </summary>
        private static List<ActivityEntity> CreateSampleActivities()
        {
            var now = DateTime.UtcNow;
            return new List<ActivityEntity>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    ActivityId = "ACT001",
                    Title = "Inter-House Sports Day",
                    Description = "Annual sports competition between school houses featuring athletics, football, volleyball, and basketball events. All students are encouraged to participate and support their houses.",
                    Date = now.AddDays(14),
                    StartTime = now.AddDays(14).Date.AddHours(8),
                    EndTime = now.AddDays(14).Date.AddHours(17),
                    Venue = "School Sports Complex",
                    ActivityType = "Sports",
                    Organizer = "Sports Department",
                    TargetForms = new List<string> { "Form1", "Form2", "Form3", "Form4" },
                    IsOptional = true,
                    RegistrationDeadline = now.AddDays(7),
                    MaxParticipants = 200,
                    CurrentParticipants = 45,
                    Requirements = "Sports attire, water bottle, and house colors",
                    Status = "Active",
                    CreatedBy = "system"
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    ActivityId = "ACT002",
                    Title = "Science Exhibition",
                    Description = "Students showcase their science projects and innovations. Categories include Physics, Chemistry, Biology, and Computer Science. Prizes will be awarded for the best projects.",
                    Date = now.AddDays(21),
                    StartTime = now.AddDays(21).Date.AddHours(9),
                    EndTime = now.AddDays(21).Date.AddHours(16),
                    Venue = "School Hall",
                    ActivityType = "Academic",
                    Organizer = "Science Department",
                    TargetForms = new List<string> { "Form3", "Form4" },
                    IsOptional = true,
                    RegistrationDeadline = now.AddDays(14),
                    MaxParticipants = 100,
                    CurrentParticipants = 32,
                    Requirements = "Science project, display materials, and presentation skills",
                    Status = "Active",
                    CreatedBy = "system"
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    ActivityId = "ACT003",
                    Title = "Cultural Day Celebration",
                    Description = "Celebration of Kenya's diverse cultures through traditional dances, songs, food, and dress. Students will represent different communities and their rich heritage.",
                    Date = now.AddDays(35),
                    StartTime = now.AddDays(35).Date.AddHours(10),
                    EndTime = now.AddDays(35).Date.AddHours(15),
                    Venue = "School Amphitheater",
                    ActivityType = "Cultural",
                    Organizer = "Humanities Department",
                    TargetForms = new List<string> { "Form1", "Form2", "Form3", "Form4" },
                    IsOptional = false,
                    RegistrationDeadline = now.AddDays(28),
                    MaxParticipants = 300,
                    CurrentParticipants = 89,
                    Requirements = "Traditional attire representing various Kenyan communities",
                    Status = "Active",
                    CreatedBy = "system"
                }
            };
        }

        /// <summary>
        /// Create sample fees for a student
        /// </summary>
        private static FeesEntity CreateSampleFeesForStudent(string studentId)
        {
            return new FeesEntity
            {
                Id = Guid.NewGuid().ToString(),
                StudentId = studentId,
                TotalFees = 80000m,
                PaidAmount = 55000m,
                Balance = 25000m,
                Year = DateTime.UtcNow.Year,
                Term = GetCurrentTerm(),
                DueDate = DateTime.UtcNow.AddDays(30),
                PaymentStatus = "Partial",
                LastPaymentDate = DateTime.UtcNow.AddDays(-15),
                DiscountAmount = 0,
                CreatedBy = "system"
            };
        }

        /// <summary>
        /// Create sample announcements
        /// </summary>
        private static List<AnnouncementEntity> CreateSampleAnnouncements()
        {
            var now = DateTime.UtcNow;
            return new List<AnnouncementEntity>
            {
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Term 1 Examination Timetable",
                    Content = "The examination timetable for Term 1 has been released. Students are advised to check the notice board for their specific examination dates and times. Examinations will commence on Monday, March 25th, 2025. All students must be present 30 minutes before their scheduled exam time.",
                    AnnouncementType = "Academic",
                    Priority = "High",
                    TargetAudience = new List<string> { "Students", "Teachers", "Parents" },
                    TargetForms = new List<string> { "Form1", "Form2", "Form3", "Form4" },
                    IsPublished = true,
                    PublishDate = now.AddDays(-2),
                    ExpiryDate = now.AddDays(30),
                    CreatedBy = "system"
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "Fee Payment Reminder",
                    Content = "This is a reminder to all parents and guardians that school fees for Term 1 are due by the end of this month. Students with outstanding fees may not be allowed to sit for examinations. For any fee-related queries, please contact the school bursar during office hours.",
                    AnnouncementType = "Fee",
                    Priority = "Normal",
                    TargetAudience = new List<string> { "Parents", "Students" },
                    TargetForms = new List<string> { "Form1", "Form2", "Form3", "Form4" },
                    IsPublished = true,
                    PublishDate = now.AddDays(-5),
                    ExpiryDate = now.AddDays(15),
                    CreatedBy = "system"
                },
                new()
                {
                    Id = Guid.NewGuid().ToString(),
                    Title = "COVID-19 Safety Protocols",
                    Content = "All students, staff, and visitors are reminded to observe COVID-19 safety protocols while on school premises. This includes wearing masks in designated areas, maintaining social distance, and regular hand washing. Any student feeling unwell should report to the school nurse immediately.",
                    AnnouncementType = "General",
                    Priority = "High",
                    TargetAudience = new List<string> { "Students", "Teachers", "Staff", "Parents" },
                    TargetForms = new List<string> { "Form1", "Form2", "Form3", "Form4" },
                    IsPublished = true,
                    PublishDate = now.AddDays(-1),
                    ExpiryDate = now.AddDays(60),
                    CreatedBy = "system"
                }
            };
        }

        /// <summary>
        /// Get current academic term based on month
        /// </summary>
        private static int GetCurrentTerm()
        {
            var month = DateTime.UtcNow.Month;
            return month switch
            {
                >= 1 and <= 4 => 1,    // January - April: Term 1
                >= 5 and <= 8 => 2,    // May - August: Term 2
                >= 9 and <= 12 => 3,   // September - December: Term 3
                _ => 1
            };
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    _initializationSemaphore?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error during DatabaseInitializer disposal");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }

        ~DatabaseInitializer()
        {
            Dispose(false);
        }

        #endregion
    }

    #region Supporting Classes and Models

    /// <summary>
    /// Result of database initialization operation
    /// </summary>
    public class InitializationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Progress information for initialization
    /// </summary>
    public class InitializationProgress
    {
        public string Step { get; set; } = string.Empty;
        public int Percentage { get; set; }
        public string Details { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Current initialization status
    /// </summary>
    public class InitializationStatus
    {
        public bool TablesVerified { get; set; }
        public bool GradingSystemMigrated { get; set; }
        public bool DefaultUsersCreated { get; set; }
        public bool SampleDataMigrated { get; set; }
        public int LastMigrationVersion { get; set; }
        public int CurrentMigrationVersion { get; set; }
        public bool IsUpToDate { get; set; }
        public DateTime? LastInitializationDate { get; set; }

        /// <summary>
        /// Check if full initialization is complete
        /// </summary>
        public bool IsFullyInitialized => TablesVerified && GradingSystemMigrated && DefaultUsersCreated && IsUpToDate;

        /// <summary>
        /// Get initialization completion percentage
        /// </summary>
        public int CompletionPercentage
        {
            get
            {
                var completed = 0;
                if (TablesVerified) completed += 25;
                if (GradingSystemMigrated) completed += 25;
                if (DefaultUsersCreated) completed += 25;
                if (IsUpToDate) completed += 25;
                return completed;
            }
        }
    }

    /// <summary>
    /// Generic operation result
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public string? Details { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static OperationResult Success(string? details = null)
        {
            return new OperationResult
            {
                Success = true,
                Details = details
            };
        }

        public static OperationResult Failure(string errorMessage, string? details = null)
        {
            return new OperationResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Details = details
            };
        }
    }

    /// <summary>
    /// Database health check result
    /// </summary>
    public class DatabaseHealthResult
    {
        public bool IsHealthy { get; set; }
        public string Status { get; set; } = string.Empty;
        public Dictionary<string, bool> TableStatus { get; set; } = new();
        public TimeSpan ResponseTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CheckTime { get; set; } = DateTime.UtcNow;
    }

    #endregion

    #region Extension Methods for DatabaseInitializer

    /// <summary>
    /// Extension methods for DatabaseInitializer
    /// </summary>
    public static class DatabaseInitializerExtensions
    {
        /// <summary>
        /// Initialize database with progress callback
        /// </summary>
        public static async Task<InitializationResult> InitializeWithProgressAsync(
            this DatabaseInitializer initializer,
            Action<string> progressCallback)
        {
            var progress = new Progress<InitializationProgress>(p => progressCallback(p.Step));
            return await initializer.InitializeAsync(progress);
        }

        /// <summary>
        /// Check if initialization is needed
        /// </summary>
        public static bool IsInitializationNeeded(this DatabaseInitializer initializer)
        {
            var status = initializer.GetInitializationStatus();
            return !status.IsFullyInitialized;
        }

        /// <summary>
        /// Get initialization summary
        /// </summary>
        public static string GetInitializationSummary(this DatabaseInitializer initializer)
        {
            var status = initializer.GetInitializationStatus();
            return $"Database Initialization Status:\n" +
                   $"- Tables Verified: {(status.TablesVerified ? "✓" : "✗")}\n" +
                   $"- Grading System: {(status.GradingSystemMigrated ? "✓" : "✗")}\n" +
                   $"- Default Users: {(status.DefaultUsersCreated ? "✓" : "✗")}\n" +
                   $"- Sample Data: {(status.SampleDataMigrated ? "✓" : "✗")}\n" +
                   $"- Up to Date: {(status.IsUpToDate ? "✓" : "✗")}\n" +
                   $"- Completion: {status.CompletionPercentage}%";
        }
    }

    #endregion
}