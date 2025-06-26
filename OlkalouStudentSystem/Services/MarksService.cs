// ===============================
// Services/MarksService.cs - Complete Marks Management Service
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Models.Data;
using OlkalouStudentSystem.Services;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;

namespace OlkalouStudentSystem.Services
{
    /// <summary>
    /// Comprehensive service for managing student marks, grades, and academic performance
    /// </summary>
    public class MarksService : IDisposable
    {
        #region Fields and Dependencies

        private readonly SupabaseService _supabaseService;
        private readonly ILogger<MarksService>? _logger;
        private readonly SemaphoreSlim _operationSemaphore = new(1, 1);

        // Grading configuration
        private readonly Dictionary<string, (decimal min, decimal max, decimal points)> _gradingScale = new()
        {
            { "A", (80, 100, 12) },
            { "A-", (75, 79, 11) },
            { "B+", (70, 74, 10) },
            { "B", (65, 69, 9) },
            { "B-", (60, 64, 8) },
            { "C+", (55, 59, 7) },
            { "C", (50, 54, 6) },
            { "C-", (45, 49, 5) },
            { "D+", (40, 44, 4) },
            { "D", (35, 39, 3) },
            { "D-", (30, 34, 2) },
            { "E", (0, 29, 1) }
        };

        #endregion

        #region Constructor

        public MarksService(SupabaseService? supabaseService = null, ILogger<MarksService>? logger = null)
        {
            _supabaseService = supabaseService ?? SupabaseService.Instance;
            _logger = logger;
        }

        #endregion

        #region Mark Entry and Management

        /// <summary>
        /// Enter or update marks for a student in a specific subject
        /// </summary>
        public async Task<OperationResult<MarkEntry>> EnterMarksAsync(MarkEntryRequest request)
        {
            if (request == null)
                return OperationResult<MarkEntry>.Failure("Mark entry request cannot be null");

            await _operationSemaphore.WaitAsync();

            try
            {
                // Validate request
                var validationResult = ValidateMarkEntryRequest(request);
                if (!validationResult.IsValid)
                {
                    return OperationResult<MarkEntry>.Failure(validationResult.ErrorMessage);
                }

                // Check teacher authorization
                if (!await IsAuthorizedToEnterMarksAsync(request.TeacherId, request.SubjectName, request.ClassName))
                {
                    return OperationResult<MarkEntry>.Failure("Not authorized to enter marks for this subject/class");
                }

                // Check if mark already exists
                var existingMark = await GetExistingMarkAsync(
                    request.StudentId, request.SubjectName, request.Term, request.Year, request.ExamType ?? "End of Term");

                MarkEntity markEntity;

                if (existingMark != null)
                {
                    // Update existing mark
                    markEntity = await UpdateExistingMarkAsync(existingMark, request);
                }
                else
                {
                    // Create new mark entry
                    markEntity = await CreateNewMarkAsync(request);
                }

                // Convert to model and return
                var markEntry = await ConvertToMarkEntryAsync(markEntity);

                _logger?.LogInformation("Marks entered successfully for student {StudentId} in {Subject}",
                    request.StudentId, request.SubjectName);

                return OperationResult<MarkEntry>.Success(markEntry, "Marks entered successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error entering marks for student {StudentId}", request.StudentId);
                return OperationResult<MarkEntry>.Failure($"Failed to enter marks: {ex.Message}");
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// Get marks for a specific student
        /// </summary>
        public async Task<OperationResult<List<MarkEntry>>> GetStudentMarksAsync(
            string studentId, int year, int? term = null)
        {
            try
            {
                var query = _supabaseService.Client
                    .From<MarkEntity>()
                    .Where(x => x.StudentId == studentId && x.Year == year);

                if (term.HasValue)
                {
                    query = query.Where(x => x.Term == term.Value);
                }

                var marksResponse = await query
                    .Order(x => x.Subject, Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Get();

                var markEntries = new List<MarkEntry>();

                if (marksResponse?.Models != null)
                {
                    foreach (var markEntity in marksResponse.Models)
                    {
                        var markEntry = await ConvertToMarkEntryAsync(markEntity);
                        markEntries.Add(markEntry);
                    }
                }

                return OperationResult<List<MarkEntry>>.Success(markEntries);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting marks for student {StudentId}", studentId);
                return OperationResult<List<MarkEntry>>.Failure($"Failed to get student marks: {ex.Message}");
            }
        }

        /// <summary>
        /// Get marks for a specific class and subject
        /// </summary>
        public async Task<OperationResult<List<MarkEntry>>> GetClassMarksAsync(
            string className, string subjectName, int term, int year)
        {
            try
            {
                // Get all students in the class
                var studentsResponse = await _supabaseService.Client
                    .From<StudentEntity>()
                    .Where(x => x.Class == className && x.IsActive && x.Year == year)
                    .Get();

                if (studentsResponse?.Models == null || !studentsResponse.Models.Any())
                {
                    return OperationResult<List<MarkEntry>>.Success(new List<MarkEntry>());
                }

                var markEntries = new List<MarkEntry>();

                foreach (var student in studentsResponse.Models)
                {
                    // Get marks for each student
                    var markEntity = await GetExistingMarkAsync(
                        student.Id, subjectName, term, year, "End of Term");

                    if (markEntity != null)
                    {
                        var markEntry = await ConvertToMarkEntryAsync(markEntity);
                        markEntry.StudentName = student.FullName;
                        markEntry.AdmissionNo = student.AdmissionNo;
                        markEntries.Add(markEntry);
                    }
                    else
                    {
                        // Create placeholder entry for students without marks
                        markEntries.Add(new MarkEntry
                        {
                            StudentId = student.Id,
                            StudentName = student.FullName,
                            AdmissionNo = student.AdmissionNo,
                            SubjectName = subjectName,
                            ClassName = className,
                            Term = term.ToString(),
                            AcademicYear = year
                        });
                    }
                }

                // Sort by admission number
                markEntries = markEntries.OrderBy(x => x.AdmissionNo).ToList();

                return OperationResult<List<MarkEntry>>.Success(markEntries);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting class marks for {ClassName} - {Subject}", className, subjectName);
                return OperationResult<List<MarkEntry>>.Failure($"Failed to get class marks: {ex.Message}");
            }
        }

        /// <summary>
        /// Approve marks entered by a teacher
        /// </summary>
        public async Task<OperationResult> ApproveMarksAsync(string markId, string approvedBy)
        {
            try
            {
                var markEntity = await _supabaseService.Client
                    .From<MarkEntity>()
                    .Where(x => x.Id == markId)
                    .Single();

                if (markEntity == null)
                {
                    return OperationResult.Failure("Mark entry not found");
                }

                markEntity.IsApproved = true;
                markEntity.ApprovedBy = approvedBy;
                markEntity.ApprovalDate = DateTime.UtcNow;
                markEntity.UpdatedAt = DateTime.UtcNow;

                await markEntity.Update<MarkEntity>();

                _logger?.LogInformation("Marks approved for mark ID {MarkId} by {ApprovedBy}", markId, approvedBy);

                return OperationResult.Success("Marks approved successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error approving marks {MarkId}", markId);
                return OperationResult.Failure($"Failed to approve marks: {ex.Message}");
            }
        }

        #endregion

        #region Performance Analysis

        /// <summary>
        /// Calculate class performance statistics
        /// </summary>
        public async Task<OperationResult<ClassPerformance>> CalculateClassPerformanceAsync(
            string className, int term, int year)
        {
            try
            {
                // Get all approved marks for the class
                var studentsResponse = await _supabaseService.Client
                    .From<StudentEntity>()
                    .Where(x => x.Class == className && x.IsActive && x.Year == year)
                    .Get();

                if (studentsResponse?.Models == null || !studentsResponse.Models.Any())
                {
                    return OperationResult<ClassPerformance>.Failure("No students found in class");
                }

                var studentIds = studentsResponse.Models.Select(s => s.Id).ToList();

                var marksResponse = await _supabaseService.Client
                    .From<MarkEntity>()
                    .Where(x => studentIds.Contains(x.StudentId) && x.Term == term && x.Year == year && x.IsApproved)
                    .Get();

                var classPerformance = new ClassPerformance
                {
                    ClassId = className,
                    ClassName = className,
                    AcademicYear = year,
                    Term = term.ToString(),
                    TotalStudents = studentsResponse.Models.Count,
                    SubjectMeans = new Dictionary<string, double>(),
                    StudentRankings = new List<StudentPerformance>(),
                    GeneratedDate = DateTime.UtcNow
                };

                if (marksResponse?.Models != null && marksResponse.Models.Any())
                {
                    // Calculate subject means
                    var subjectGroups = marksResponse.Models.GroupBy(m => m.Subject);
                    foreach (var subjectGroup in subjectGroups)
                    {
                        var subjectMean = subjectGroup.Average(m => (double)m.TotalMarks);
                        classPerformance.SubjectMeans[subjectGroup.Key] = Math.Round(subjectMean, 2);
                    }

                    // Calculate student rankings
                    var studentGroups = marksResponse.Models.GroupBy(m => m.StudentId);
                    var studentPerformances = new List<StudentPerformance>();

                    foreach (var studentGroup in studentGroups)
                    {
                        var student = studentsResponse.Models.FirstOrDefault(s => s.Id == studentGroup.Key);
                        if (student != null)
                        {
                            var totalMarks = studentGroup.Sum(m => (double)m.TotalMarks);
                            var meanScore = studentGroup.Average(m => (double)m.TotalMarks);

                            var studentPerformance = new StudentPerformance
                            {
                                StudentId = student.Id,
                                StudentName = student.FullName,
                                AdmissionNo = student.AdmissionNo,
                                TotalMarks = totalMarks,
                                MeanScore = Math.Round(meanScore, 2),
                                Term = term.ToString(),
                                AcademicYear = year,
                                SubjectPerformances = new List<SubjectPerformance>()
                            };

                            // Add subject performances
                            foreach (var mark in studentGroup)
                            {
                                studentPerformance.SubjectPerformances.Add(new SubjectPerformance
                                {
                                    Subject = mark.Subject,
                                    Marks = (double)mark.TotalMarks,
                                    Grade = mark.Grade,
                                    Position = 0 // Will be calculated later
                                });
                            }

                            studentPerformances.Add(studentPerformance);
                        }
                    }

                    // Sort students by mean score and assign positions
                    studentPerformances = studentPerformances
                        .OrderByDescending(sp => sp.MeanScore)
                        .ToList();

                    for (int i = 0; i < studentPerformances.Count; i++)
                    {
                        studentPerformances[i].OverallPosition = i + 1;
                    }

                    classPerformance.StudentRankings = studentPerformances;
                    classPerformance.ClassMean = Math.Round(studentPerformances.Average(sp => sp.MeanScore), 2);
                }

                return OperationResult<ClassPerformance>.Success(classPerformance);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating class performance for {ClassName}", className);
                return OperationResult<ClassPerformance>.Failure($"Failed to calculate class performance: {ex.Message}");
            }
        }

        /// <summary>
        /// Get student performance trends
        /// </summary>
        public async Task<OperationResult<StudentPerformanceTrend>> GetStudentPerformanceTrendAsync(
            string studentId, int year)
        {
            try
            {
                var marksResponse = await _supabaseService.Client
                    .From<MarkEntity>()
                    .Where(x => x.StudentId == studentId && x.Year == year && x.IsApproved)
                    .Order(x => x.Term, Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Get();

                var trend = new StudentPerformanceTrend
                {
                    StudentId = studentId,
                    Year = year,
                    TermPerformances = new List<TermPerformance>()
                };

                if (marksResponse?.Models != null)
                {
                    var termGroups = marksResponse.Models.GroupBy(m => m.Term).OrderBy(g => g.Key);

                    foreach (var termGroup in termGroups)
                    {
                        var termMarks = termGroup.ToList();
                        var termMean = termMarks.Average(m => (double)m.TotalMarks);

                        var termPerformance = new TermPerformance
                        {
                            Term = termGroup.Key,
                            MeanScore = Math.Round(termMean, 2),
                            SubjectCount = termMarks.Count,
                            SubjectPerformances = termMarks.Select(m => new SubjectPerformance
                            {
                                Subject = m.Subject,
                                Marks = (double)m.TotalMarks,
                                Grade = m.Grade,
                                TeacherName = "TBD" // You might want to fetch this
                            }).ToList()
                        };

                        trend.TermPerformances.Add(termPerformance);
                    }

                    // Calculate improvement trend
                    if (trend.TermPerformances.Count > 1)
                    {
                        var firstTerm = trend.TermPerformances.First().MeanScore;
                        var lastTerm = trend.TermPerformances.Last().MeanScore;
                        trend.OverallTrend = lastTerm > firstTerm ? "Improving" :
                                           lastTerm < firstTerm ? "Declining" : "Stable";
                        trend.ImprovementPercentage = Math.Round(((lastTerm - firstTerm) / firstTerm) * 100, 2);
                    }
                }

                return OperationResult<StudentPerformanceTrend>.Success(trend);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting performance trend for student {StudentId}", studentId);
                return OperationResult<StudentPerformanceTrend>.Failure($"Failed to get performance trend: {ex.Message}");
            }
        }

        #endregion

        #region Grade Management

        /// <summary>
        /// Calculate grade from percentage
        /// </summary>
        public string CalculateGrade(decimal percentage)
        {
            foreach (var grade in _gradingScale)
            {
                if (percentage >= grade.Value.min && percentage <= grade.Value.max)
                {
                    return grade.Key;
                }
            }
            return "E"; // Default grade
        }

        /// <summary>
        /// Get points for a grade
        /// </summary>
        public decimal GetPointsForGrade(string grade)
        {
            return _gradingScale.TryGetValue(grade, out var gradeInfo) ? gradeInfo.points : 1;
        }

        /// <summary>
        /// Update grading system
        /// </summary>
        public async Task<OperationResult> UpdateGradingSystemAsync(List<GradingScheme> gradingSchemes)
        {
            try
            {
                // Clear existing grading system
                var existingGrades = await _supabaseService.Client
                    .From<GradingSystemEntity>()
                    .Get();

                if (existingGrades?.Models != null)
                {
                    foreach (var grade in existingGrades.Models)
                    {
                        await grade.Delete<GradingSystemEntity>();
                    }
                }

                // Insert new grading system
                foreach (var scheme in gradingSchemes)
                {
                    var gradingEntity = new GradingSystemEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        Grade = scheme.SchemeName,
                        MinPercentage = (decimal)scheme.OpeningPercentage,
                        MaxPercentage = (decimal)scheme.MidTermPercentage,
                        Points = 0, // You might want to calculate this
                        CreatedBy = "system",
                        IsActive = scheme.IsActive
                    };

                    await _supabaseService.Client.From<GradingSystemEntity>().Insert(gradingEntity);
                }

                return OperationResult.Success("Grading system updated successfully");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating grading system");
                return OperationResult.Failure($"Failed to update grading system: {ex.Message}");
            }
        }

        #endregion

        #region Report Generation

        /// <summary>
        /// Generate student report card
        /// </summary>
        public async Task<OperationResult<StudentReportCard>> GenerateReportCardAsync(
            string studentId, int term, int year)
        {
            try
            {
                // Get student details
                var student = await _supabaseService.Client
                    .From<StudentEntity>()
                    .Where(x => x.Id == studentId)
                    .Single();

                if (student == null)
                {
                    return OperationResult<StudentReportCard>.Failure("Student not found");
                }

                // Get marks for the term
                var marksResponse = await _supabaseService.Client
                    .From<MarkEntity>()
                    .Where(x => x.StudentId == studentId && x.Term == term && x.Year == year && x.IsApproved)
                    .Get();

                var reportCard = new StudentReportCard
                {
                    StudentId = student.StudentId ?? student.Id,
                    StudentName = student.FullName,
                    AdmissionNo = student.AdmissionNo,
                    Class = student.Class,
                    Term = term,
                    Year = year,
                    GeneratedDate = DateTime.UtcNow,
                    SubjectResults = new List<SubjectResult>()
                };

                if (marksResponse?.Models != null && marksResponse.Models.Any())
                {
                    decimal totalMarks = 0;
                    int subjectCount = 0;

                    foreach (var mark in marksResponse.Models)
                    {
                        var subjectResult = new SubjectResult
                        {
                            Subject = mark.Subject,
                            OpeningMarks = mark.OpeningMarks ?? 0,
                            MidTermMarks = mark.MidtermMarks ?? 0,
                            FinalExamMarks = mark.FinalExamMarks ?? 0,
                            TotalMarks = mark.TotalMarks,
                            Grade = mark.Grade,
                            Points = mark.Points ?? 0,
                            TeacherComments = mark.TeacherComments ?? ""
                        };

                        reportCard.SubjectResults.Add(subjectResult);
                        totalMarks += mark.TotalMarks;
                        subjectCount++;
                    }

                    if (subjectCount > 0)
                    {
                        reportCard.MeanScore = Math.Round(totalMarks / subjectCount, 2);
                        reportCard.TotalPoints = reportCard.SubjectResults.Sum(sr => sr.Points);
                        reportCard.OverallGrade = CalculateGrade(reportCard.MeanScore);
                    }

                    // Get class position
                    var classPerformanceResult = await CalculateClassPerformanceAsync(student.Class, term, year);
                    if (classPerformanceResult.Success && classPerformanceResult.Data != null)
                    {
                        var studentRanking = classPerformanceResult.Data.StudentRankings
                            .FirstOrDefault(sr => sr.StudentId == studentId);

                        if (studentRanking != null)
                        {
                            reportCard.ClassPosition = studentRanking.OverallPosition;
                            reportCard.ClassSize = classPerformanceResult.Data.TotalStudents;
                        }
                    }
                }

                return OperationResult<StudentReportCard>.Success(reportCard);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating report card for student {StudentId}", studentId);
                return OperationResult<StudentReportCard>.Failure($"Failed to generate report card: {ex.Message}");
            }
        }

        #endregion

        #region Private Helper Methods

        private ValidationResult ValidateMarkEntryRequest(MarkEntryRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.StudentId))
                return new ValidationResult { IsValid = false, ErrorMessage = "Student ID is required" };

            if (string.IsNullOrWhiteSpace(request.SubjectName))
                return new ValidationResult { IsValid = false, ErrorMessage = "Subject name is required" };

            if (string.IsNullOrWhiteSpace(request.TeacherId))
                return new ValidationResult { IsValid = false, ErrorMessage = "Teacher ID is required" };

            if (request.Term < 1 || request.Term > 3)
                return new ValidationResult { IsValid = false, ErrorMessage = "Term must be between 1 and 3" };

            if (request.Year < 2020 || request.Year > DateTime.UtcNow.Year + 1)
                return new ValidationResult { IsValid = false, ErrorMessage = "Invalid year" };

            // Validate mark ranges
            if (request.OpeningMarks.HasValue && (request.OpeningMarks < 0 || request.OpeningMarks > 100))
                return new ValidationResult { IsValid = false, ErrorMessage = "Opening marks must be between 0 and 100" };

            if (request.MidTermMarks.HasValue && (request.MidTermMarks < 0 || request.MidTermMarks > 100))
                return new ValidationResult { IsValid = false, ErrorMessage = "Mid-term marks must be between 0 and 100" };

            if (request.FinalExamMarks.HasValue && (request.FinalExamMarks < 0 || request.FinalExamMarks > 100))
                return new ValidationResult { IsValid = false, ErrorMessage = "Final exam marks must be between 0 and 100" };

            return new ValidationResult { IsValid = true };
        }

        private async Task<bool> IsAuthorizedToEnterMarksAsync(string teacherId, string subject, string className)
        {
            try
            {
                var teacher = await _supabaseService.Client
                    .From<TeacherEntity>()
                    .Where(x => x.TeacherId == teacherId && x.IsActive)
                    .Single();

                if (teacher == null) return false;

                // Check if teacher is assigned to the subject and class
                return teacher.Subjects.Contains(subject) &&
                       (teacher.AssignedForms.Contains(className) || teacher.ClassTeacherFor == className);
            }
            catch
            {
                return false;
            }
        }

        private async Task<MarkEntity?> GetExistingMarkAsync(
            string studentId, string subject, int term, int year, string examType)
        {
            try
            {
                return await _supabaseService.Client
                    .From<MarkEntity>()
                    .Where(x => x.StudentId == studentId &&
                               x.Subject == subject &&
                               x.Term == term &&
                               x.Year == year &&
                               x.ExamType == examType)
                    .Single();
            }
            catch
            {
                return null;
            }
        }

        private async Task<MarkEntity> UpdateExistingMarkAsync(MarkEntity existingMark, MarkEntryRequest request)
        {
            existingMark.OpeningMarks = request.OpeningMarks;
            existingMark.MidtermMarks = request.MidTermMarks;
            existingMark.FinalExamMarks = request.FinalExamMarks;
            existingMark.TeacherComments = request.Comments;
            existingMark.UpdatedAt = DateTime.UtcNow;

            CalculateMarkTotals(existingMark);
            await existingMark.Update<MarkEntity>();

            return existingMark;
        }

        private async Task<MarkEntity> CreateNewMarkAsync(MarkEntryRequest request)
        {
            var markEntity = new MarkEntity
            {
                Id = Guid.NewGuid().ToString(),
                StudentId = request.StudentId,
                Subject = request.SubjectName,
                OpeningMarks = request.OpeningMarks,
                MidtermMarks = request.MidTermMarks,
                FinalExamMarks = request.FinalExamMarks,
                Term = request.Term,
                Year = request.Year,
                ExamType = request.ExamType ?? "End of Term",
                TeacherId = request.TeacherId,
                TeacherComments = request.Comments,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            CalculateMarkTotals(markEntity);
            await _supabaseService.Client.From<MarkEntity>().Insert(markEntity);

            return markEntity;
        }

        private void CalculateMarkTotals(MarkEntity mark)
        {
            // Standard Kenyan grading: Opening 15%, Mid-term 15%, Final 70%
            var opening = mark.OpeningMarks ?? 0;
            var midterm = mark.MidtermMarks ?? 0;
            var final = mark.FinalExamMarks ?? 0;

            mark.TotalMarks = (opening * 0.15m) + (midterm * 0.15m) + (final * 0.70m);
            mark.Percentage = mark.TotalMarks; // Assuming max marks is 100
            mark.Grade = CalculateGrade(mark.Percentage);
            mark.Points = GetPointsForGrade(mark.Grade);
        }

        private async Task<MarkEntry> ConvertToMarkEntryAsync(MarkEntity markEntity)
        {
            // Get student and teacher details
            var student = await _supabaseService.Client
                .From<StudentEntity>()
                .Where(x => x.Id == markEntity.StudentId)
                .Single();

            var teacher = await _supabaseService.Client
                .From<TeacherEntity>()
                .Where(x => x.TeacherId == markEntity.TeacherId)
                .Single();

            return new MarkEntry
            {
                MarkId = markEntity.Id,
                StudentId = markEntity.StudentId,
                StudentName = student?.FullName ?? "Unknown",
                AdmissionNo = student?.AdmissionNo ?? "Unknown",
                SubjectName = markEntity.Subject,
                ClassName = student?.Class ?? "Unknown",
                AcademicYear = markEntity.Year,
                Term = markEntity.Term.ToString(),
                OpeningMarks = (double)(markEntity.OpeningMarks ?? 0),
                MidTermMarks = (double)(markEntity.MidtermMarks ?? 0),
                EndTermMarks = (double)(markEntity.FinalExamMarks ?? 0),
                TotalMarks = (double)markEntity.TotalMarks,
                Grade = markEntity.Grade,
                Position = 0, // Calculate separately if needed
                TeacherId = markEntity.TeacherId,
                TeacherName = teacher?.FullName ?? "Unknown",
                DateEntered = markEntity.CreatedAt,
                DateModified = markEntity.UpdatedAt,
                IsSubmitted = true,
                IsApproved = markEntity.IsApproved,
                Comments = markEntity.TeacherComments ?? ""
            };
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            _operationSemaphore?.Dispose();
        }

        #endregion
    }

    #region Supporting Models and Classes

    /// <summary>
    /// Request model for entering marks
    /// </summary>
    public class MarkEntryRequest
    {
        [Required]
        public string StudentId { get; set; } = string.Empty;

        [Required]
        public string SubjectName { get; set; } = string.Empty;

        [Required]
        public string TeacherId { get; set; } = string.Empty;

        [Required]
        public string ClassName { get; set; } = string.Empty;

        [Range(1, 3)]
        public int Term { get; set; }

        [Range(2020, 2050)]
        public int Year { get; set; }

        [Range(0, 100)]
        public decimal? OpeningMarks { get; set; }

        [Range(0, 100)]
        public decimal? MidTermMarks { get; set; }

        [Range(0, 100)]
        public decimal? FinalExamMarks { get; set; }

        public string? ExamType { get; set; } = "End of Term";

        [StringLength(500)]
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Student performance trend analysis
    /// </summary>
    public class StudentPerformanceTrend
    {
        public string StudentId { get; set; } = string.Empty;
        public int Year { get; set; }
        public string OverallTrend { get; set; } = string.Empty; // Improving, Declining, Stable
        public double ImprovementPercentage { get; set; }
        public List<TermPerformance> TermPerformances { get; set; } = new();
    }

    /// <summary>
    /// Performance data for a specific term
    /// </summary>
    public class TermPerformance
    {
        public int Term { get; set; }
        public double MeanScore { get; set; }
        public int SubjectCount { get; set; }
        public List<SubjectPerformance> SubjectPerformances { get; set; } = new();
    }

    /// <summary>
    /// Subject performance data
    /// </summary>
    public class SubjectPerformance
    {
        public string Subject { get; set; } = string.Empty;
        public double Marks { get; set; }
        public string Grade { get; set; } = string.Empty;
        public int Position { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string? Trend { get; set; }
        public int PreviousGrade { get; set; }
        public int CurrentGrade { get; set; }
    }

    /// <summary>
    /// Student performance data
    /// </summary>
    public class StudentPerformance
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public double TotalMarks { get; set; }
        public double MeanScore { get; set; }
        public string Term { get; set; } = string.Empty;
        public int AcademicYear { get; set; }
        public int OverallPosition { get; set; }
        public List<SubjectPerformance> SubjectPerformances { get; set; } = new();
    }

    /// <summary>
    /// Class performance analysis
    /// </summary>
    public class ClassPerformance
    {
        public string ClassId { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int AcademicYear { get; set; }
        public string Term { get; set; } = string.Empty;
        public int TotalStudents { get; set; }
        public double ClassMean { get; set; }
        public Dictionary<string, double> SubjectMeans { get; set; } = new();
        public List<StudentPerformance> StudentRankings { get; set; } = new();
        public DateTime GeneratedDate { get; set; }
    }

    /// <summary>
    /// Student report card model
    /// </summary>
    public class StudentReportCard
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public int Term { get; set; }
        public int Year { get; set; }
        public decimal MeanScore { get; set; }
        public decimal TotalPoints { get; set; }
        public string OverallGrade { get; set; } = string.Empty;
        public int ClassPosition { get; set; }
        public int ClassSize { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<SubjectResult> SubjectResults { get; set; } = new();
        public string ClassTeacherComments { get; set; } = string.Empty;
        public string PrincipalComments { get; set; } = string.Empty;
        public DateTime NextTermBegins { get; set; }
        public bool PromotionStatus { get; set; } = true;
    }

    /// <summary>
    /// Subject result for report card
    /// </summary>
    public class SubjectResult
    {
        public string Subject { get; set; } = string.Empty;
        public decimal OpeningMarks { get; set; }
        public decimal MidTermMarks { get; set; }
        public decimal FinalExamMarks { get; set; }
        public decimal TotalMarks { get; set; }
        public string Grade { get; set; } = string.Empty;
        public decimal Points { get; set; }
        public int SubjectPosition { get; set; }
        public string TeacherComments { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Grading scheme model
    /// </summary>
    public class GradingScheme
    {
        public string SchemeName { get; set; } = string.Empty;
        public double OpeningPercentage { get; set; }
        public double MidTermPercentage { get; set; }
        public double FinalPercentage { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Mark entry model
    /// </summary>
    public class MarkEntry
    {
        public string MarkId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string AdmissionNo { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public int AcademicYear { get; set; }
        public string Term { get; set; } = string.Empty;
        public double OpeningMarks { get; set; }
        public double MidTermMarks { get; set; }
        public double EndTermMarks { get; set; }
        public double TotalMarks { get; set; }
        public string Grade { get; set; } = string.Empty;
        public int Position { get; set; }
        public string TeacherId { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public DateTime DateEntered { get; set; }
        public DateTime? DateModified { get; set; }
        public bool IsSubmitted { get; set; }
        public bool IsApproved { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

    /// <summary>
    /// Operation result wrapper
    /// </summary>
    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static OperationResult Success(string message = "Operation completed successfully")
        {
            return new OperationResult { Success = true, Message = message };
        }

        public static OperationResult Failure(string message, string errorCode = "")
        {
            return new OperationResult
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// Generic operation result wrapper
    /// </summary>
    public class OperationResult<T> : OperationResult
    {
        public T? Data { get; set; }

        public static OperationResult<T> Success(T data, string message = "Operation completed successfully")
        {
            return new OperationResult<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        public static new OperationResult<T> Failure(string message, string errorCode = "")
        {
            return new OperationResult<T>
            {
                Success = false,
                Message = message,
                ErrorCode = errorCode
            };
        }
    }

    /// <summary>
    /// Validation result model
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new();
    }

    #endregion
}