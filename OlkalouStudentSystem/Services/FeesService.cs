// ===============================
// Services/FeesService.cs - Complete Fees Management Service
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Models.Data;
using OlkalouStudentSystem.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OlkalouStudentSystem.Services
{
    public class FeesService
    {
        private readonly Supabase.Client _supabaseClient;
        private readonly AuthService _authService;
        private readonly ILogger<FeesService>? _logger;

        public FeesService(AuthService authService, ILogger<FeesService>? logger = null)
        {
            _supabaseClient = SupabaseService.Instance.Client;
            _authService = authService;
            _logger = logger;
        }

        #region Fees Information Management

        /// <summary>
        /// Get fees information for a specific student
        /// </summary>
        public async Task<FeesInfo> GetStudentFeesAsync(string studentId)
        {
            try
            {
                // Get fees record for current year
                var fees = await _supabaseClient
                    .From<FeesEntity>()
                    .Where(x => x.StudentId == studentId && x.Year == DateTime.Now.Year)
                    .Single();

                if (fees == null)
                {
                    // Create new fees record if it doesn't exist
                    fees = await CreateDefaultFeesRecordAsync(studentId);
                }

                // Get payment history
                var payments = await _supabaseClient
                    .From<FeesPaymentEntity>()
                    .Where(x => x.StudentId == studentId && x.IsApproved)
                    .Order(x => x.PaymentDate, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                var feesInfo = new FeesInfo
                {
                    StudentId = fees.StudentId,
                    TotalFees = fees.TotalFees,
                    PaidAmount = fees.PaidAmount,
                    Balance = fees.Balance,
                    LastPaymentDate = fees.LastPaymentDate,
                    PaymentStatus = fees.PaymentStatus,
                    DueDate = fees.DueDate,
                    PaymentHistory = new List<PaymentRecord>()
                };

                // Convert payment entities to payment records
                if (payments?.Models != null)
                {
                    foreach (var payment in payments.Models)
                    {
                        feesInfo.PaymentHistory.Add(new PaymentRecord
                        {
                            PaymentId = payment.Id,
                            Amount = payment.Amount,
                            PaymentDate = payment.PaymentDate,
                            PaymentMethod = payment.PaymentMethod,
                            ReceiptNumber = payment.ReceiptNumber,
                            Description = payment.Description ?? "Fees Payment",
                            ReceivedBy = payment.ReceivedBy,
                            IsApproved = payment.IsApproved,
                            Status = payment.IsApproved ? "Approved" : "Pending"
                        });
                    }
                }

                // Calculate balance to ensure accuracy
                feesInfo.CalculateBalance();

                return feesInfo;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting student fees for {StudentId}", studentId);
                throw new Exception($"Failed to get fees information: {ex.Message}");
            }
        }

        /// <summary>
        /// Get fees information for current authenticated student
        /// </summary>
        public async Task<FeesInfo> GetCurrentStudentFeesAsync()
        {
            try
            {
                var currentStudentId = await GetCurrentStudentIdAsync();
                if (string.IsNullOrEmpty(currentStudentId))
                {
                    throw new UnauthorizedAccessException("No authenticated student found");
                }

                return await GetStudentFeesAsync(currentStudentId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting current student fees");
                throw;
            }
        }

        /// <summary>
        /// Get fees summary for all students (Bursar/Admin only)
        /// </summary>
        public async Task<List<FeesSummary>> GetAllStudentsFeesAsync()
        {
            try
            {
                if (!await IsBursarOrAdminAsync())
                {
                    throw new UnauthorizedAccessException("Only bursar or admin can view all student fees");
                }

                var fees = await _supabaseClient
                    .From<FeesEntity>()
                    .Where(x => x.Year == DateTime.Now.Year)
                    .Order(x => x.Balance, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                var feesSummaryList = new List<FeesSummary>();

                if (fees?.Models != null)
                {
                    foreach (var fee in fees.Models)
                    {
                        // Get student details
                        var student = await _supabaseClient
                            .From<StudentEntity>()
                            .Where(x => x.Id == fee.StudentId)
                            .Single();

                        if (student != null)
                        {
                            feesSummaryList.Add(new FeesSummary
                            {
                                StudentId = student.StudentId,
                                StudentName = student.FullName,
                                Form = student.Form,
                                Class = student.Class,
                                TotalFees = fee.TotalFees,
                                PaidAmount = fee.PaidAmount,
                                Balance = fee.Balance,
                                PaymentStatus = fee.PaymentStatus,
                                DueDate = fee.DueDate,
                                IsOverdue = fee.DueDate < DateTime.Now && fee.Balance > 0,
                                DaysOverdue = fee.DueDate < DateTime.Now ? (DateTime.Now - fee.DueDate).Days : 0
                            });
                        }
                    }
                }

                return feesSummaryList;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting all students fees");
                throw;
            }
        }

        #endregion

        #region Payment Processing

        /// <summary>
        /// Process a fee payment
        /// </summary>
        public async Task<PaymentResult> ProcessPaymentAsync(FeesPaymentRequest request)
        {
            try
            {
                // Validate payment request
                var validationResult = ValidatePaymentRequest(request);
                if (!validationResult.IsValid)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = validationResult.ErrorMessage
                    };
                }

                // Get current fees record
                var fees = await _supabaseClient
                    .From<FeesEntity>()
                    .Where(x => x.StudentId == request.StudentId && x.Year == DateTime.Now.Year)
                    .Single();

                if (fees == null)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = "Student fees record not found"
                    };
                }

                // Check if payment amount exceeds balance
                if (request.Amount > fees.Balance)
                {
                    return new PaymentResult
                    {
                        Success = false,
                        Message = $"Payment amount cannot exceed balance of KSh {fees.Balance:N2}"
                    };
                }

                // Create payment record
                var payment = new FeesPaymentEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = request.StudentId,
                    Amount = request.Amount,
                    PaymentMethod = request.PaymentMethod,
                    ReceiptNumber = GenerateReceiptNumber(),
                    TransactionId = request.TransactionId,
                    SlipImageUrl = request.SlipImageUrl,
                    IsScanned = !string.IsNullOrEmpty(request.SlipImageUrl),
                    IsApproved = ShouldAutoApprove(request),
                    Description = request.Description ?? "Fees Payment",
                    ReceivedBy = await GetCurrentUserNameAsync(),
                    BankName = request.BankName,
                    AccountNumber = request.AccountNumber,
                    PaymentDate = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                // If auto-approved, set verification details
                if (payment.IsApproved)
                {
                    payment.VerifiedBy = await GetCurrentUserIdAsync();
                    payment.VerificationDate = DateTime.UtcNow;
                }

                // Insert payment record
                await _supabaseClient.From<FeesPaymentEntity>().Insert(payment);

                // Update fees balance if approved
                if (payment.IsApproved)
                {
                    await UpdateFeesBalanceAsync(request.StudentId, request.Amount);
                }

                return new PaymentResult
                {
                    Success = true,
                    Message = payment.IsApproved ? "Payment processed and approved" : "Payment received, pending approval",
                    ReceiptNumber = payment.ReceiptNumber,
                    RequiresApproval = !payment.IsApproved,
                    PaymentId = payment.Id
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing payment");
                return new PaymentResult
                {
                    Success = false,
                    Message = $"Payment processing failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Approve a pending payment (Bursar only)
        /// </summary>
        public async Task<bool> ApprovePaymentAsync(string paymentId)
        {
            try
            {
                if (!await IsBursarOrAdminAsync())
                {
                    throw new UnauthorizedAccessException("Only bursar can approve payments");
                }

                var payment = await _supabaseClient
                    .From<FeesPaymentEntity>()
                    .Where(x => x.Id == paymentId)
                    .Single();

                if (payment == null)
                {
                    return false;
                }

                if (payment.IsApproved)
                {
                    return true; // Already approved
                }

                // Update payment approval
                payment.IsApproved = true;
                payment.VerifiedBy = await GetCurrentUserIdAsync();
                payment.VerificationDate = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await payment.Update<FeesPaymentEntity>();

                // Update fees balance
                await UpdateFeesBalanceAsync(payment.StudentId, payment.Amount);

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error approving payment {PaymentId}", paymentId);
                return false;
            }
        }

        /// <summary>
        /// Reject a pending payment (Bursar only)
        /// </summary>
        public async Task<bool> RejectPaymentAsync(string paymentId, string reason)
        {
            try
            {
                if (!await IsBursarOrAdminAsync())
                {
                    throw new UnauthorizedAccessException("Only bursar can reject payments");
                }

                var payment = await _supabaseClient
                    .From<FeesPaymentEntity>()
                    .Where(x => x.Id == paymentId)
                    .Single();

                if (payment == null)
                {
                    return false;
                }

                // Update payment status
                payment.Description += $" - REJECTED: {reason}";
                payment.VerifiedBy = await GetCurrentUserIdAsync();
                payment.VerificationDate = DateTime.UtcNow;
                payment.UpdatedAt = DateTime.UtcNow;

                await payment.Update<FeesPaymentEntity>();

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error rejecting payment {PaymentId}", paymentId);
                return false;
            }
        }

        /// <summary>
        /// Get pending payments for approval (Bursar only)
        /// </summary>
        public async Task<List<PendingPayment>> GetPendingPaymentsAsync()
        {
            try
            {
                if (!await IsBursarOrAdminAsync())
                {
                    throw new UnauthorizedAccessException("Only bursar can view pending payments");
                }

                var payments = await _supabaseClient
                    .From<FeesPaymentEntity>()
                    .Where(x => !x.IsApproved)
                    .Order(x => x.PaymentDate, Supabase.Postgrest.Constants.Ordering.Descending)
                    .Get();

                var pendingPayments = new List<PendingPayment>();

                if (payments?.Models != null)
                {
                    foreach (var payment in payments.Models)
                    {
                        // Get student details
                        var student = await _supabaseClient
                            .From<StudentEntity>()
                            .Where(x => x.Id == payment.StudentId)
                            .Single();

                        if (student != null)
                        {
                            pendingPayments.Add(new PendingPayment
                            {
                                PaymentId = payment.Id,
                                StudentId = student.StudentId,
                                StudentName = student.FullName,
                                Form = student.Form,
                                Amount = payment.Amount,
                                PaymentMethod = payment.PaymentMethod,
                                ReceiptNumber = payment.ReceiptNumber,
                                PaymentDate = payment.PaymentDate,
                                SlipImageUrl = payment.SlipImageUrl,
                                Description = payment.Description,
                                ReceivedBy = payment.ReceivedBy
                            });
                        }
                    }
                }

                return pendingPayments;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting pending payments");
                throw;
            }
        }

        #endregion

        #region Fees Administration

        /// <summary>
        /// Set fees amount for a student (Bursar/Admin only)
        /// </summary>
        public async Task<bool> SetStudentFeesAsync(string studentId, decimal totalFees, int year, int term)
        {
            try
            {
                if (!await IsBursarOrAdminAsync())
                {
                    throw new UnauthorizedAccessException("Only bursar or admin can set fees");
                }

                var existingFees = await _supabaseClient
                    .From<FeesEntity>()
                    .Where(x => x.StudentId == studentId && x.Year == year && x.Term == term)
                    .Single();

                if (existingFees != null)
                {
                    // Update existing fees
                    existingFees.TotalFees = totalFees;
                    existingFees.Balance = totalFees - existingFees.PaidAmount;
                    existingFees.UpdatedAt = DateTime.UtcNow;

                    await existingFees.Update<FeesEntity>();
                }
                else
                {
                    // Create new fees record
                    var fees = new FeesEntity
                    {
                        Id = Guid.NewGuid().ToString(),
                        StudentId = studentId,
                        TotalFees = totalFees,
                        PaidAmount = 0m,
                        Balance = totalFees,
                        Year = year,
                        Term = term,
                        DueDate = CalculateFeeDueDate(term),
                        PaymentStatus = "Pending",
                        CreatedAt = DateTime.UtcNow
                    };

                    await _supabaseClient.From<FeesEntity>().Insert(fees);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting student fees");
                return false;
            }
        }

        /// <summary>
        /// Apply discount to student fees (Bursar/Admin only)
        /// </summary>
        public async Task<bool> ApplyDiscountAsync(string studentId, decimal discountAmount, string reason)
        {
            try
            {
                if (!await IsBursarOrAdminAsync())
                {
                    throw new UnauthorizedAccessException("Only bursar or admin can apply discounts");
                }

                var fees = await _supabaseClient
                    .From<FeesEntity>()
                    .Where(x => x.StudentId == studentId && x.Year == DateTime.Now.Year)
                    .Single();

                if (fees == null)
                {
                    return false;
                }

                fees.DiscountAmount = (fees.DiscountAmount ?? 0) + discountAmount;
                fees.DiscountReason = string.IsNullOrEmpty(fees.DiscountReason)
                    ? reason
                    : $"{fees.DiscountReason}; {reason}";
                fees.Balance = fees.TotalFees - (fees.DiscountAmount ?? 0) - fees.PaidAmount;
                fees.UpdatedAt = DateTime.UtcNow;

                await fees.Update<FeesEntity>();

                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error applying discount");
                return false;
            }
        }

        /// <summary>
        /// Generate fees statement for a student
        /// </summary>
        public async Task<FeesStatement> GenerateFeesStatementAsync(string studentId, int year)
        {
            try
            {
                // Get student details
                var student = await _supabaseClient
                    .From<StudentEntity>()
                    .Where(x => x.Id == studentId)
                    .Single();

                if (student == null)
                {
                    throw new ArgumentException("Student not found");
                }

                // Get fees record
                var fees = await _supabaseClient
                    .From<FeesEntity>()
                    .Where(x => x.StudentId == studentId && x.Year == year)
                    .Single();

                if (fees == null)
                {
                    throw new ArgumentException("Fees record not found for the specified year");
                }

                // Get all payments for the year
                var payments = await _supabaseClient
                    .From<FeesPaymentEntity>()
                    .Where(x => x.StudentId == studentId && x.IsApproved)
                    .Order(x => x.PaymentDate, Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Get();

                var statement = new FeesStatement
                {
                    StudentId = student.StudentId,
                    StudentName = student.FullName,
                    Form = student.Form,
                    Class = student.Class,
                    Year = year,
                    TotalFees = fees.TotalFees,
                    DiscountAmount = fees.DiscountAmount ?? 0,
                    PaidAmount = fees.PaidAmount,
                    Balance = fees.Balance,
                    PaymentStatus = fees.PaymentStatus,
                    DueDate = fees.DueDate,
                    GeneratedDate = DateTime.UtcNow,
                    Payments = new List<PaymentRecord>()
                };

                if (payments?.Models != null)
                {
                    foreach (var payment in payments.Models)
                    {
                        statement.Payments.Add(new PaymentRecord
                        {
                            PaymentId = payment.Id,
                            Amount = payment.Amount,
                            PaymentDate = payment.PaymentDate,
                            PaymentMethod = payment.PaymentMethod,
                            ReceiptNumber = payment.ReceiptNumber,
                            Description = payment.Description ?? "Fees Payment",
                            ReceivedBy = payment.ReceivedBy,
                            IsApproved = payment.IsApproved,
                            Status = payment.IsApproved ? "Approved" : "Pending"
                        });
                    }
                }

                return statement;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating fees statement");
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Create default fees record for a student
        /// </summary>
        private async Task<FeesEntity> CreateDefaultFeesRecordAsync(string studentId)
        {
            try
            {
                var defaultFeesAmount = await GetDefaultFeesAmountAsync();
                var currentTerm = GetCurrentTerm();

                var fees = new FeesEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    StudentId = studentId,
                    TotalFees = defaultFeesAmount,
                    PaidAmount = 0m,
                    Balance = defaultFeesAmount,
                    Year = DateTime.Now.Year,
                    Term = currentTerm,
                    DueDate = CalculateFeeDueDate(currentTerm),
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                await _supabaseClient.From<FeesEntity>().Insert(fees);
                return fees;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating default fees record");
                throw;
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
                    fees.Balance = fees.TotalFees - (fees.DiscountAmount ?? 0) - fees.PaidAmount;
                    fees.LastPaymentDate = DateTime.UtcNow;
                    fees.PaymentStatus = fees.Balance <= 0 ? "Paid" :
                                        fees.PaidAmount > 0 ? "Partial" : "Pending";
                    fees.UpdatedAt = DateTime.UtcNow;

                    await fees.Update<FeesEntity>();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating fees balance");
                throw;
            }
        }

        /// <summary>
        /// Validate payment request
        /// </summary>
        private FeesValidationResult ValidatePaymentRequest(FeesPaymentRequest request)
        {
            if (request == null)
                return new FeesValidationResult { IsValid = false, ErrorMessage = "Payment request is required" };

            if (string.IsNullOrWhiteSpace(request.StudentId))
                return new FeesValidationResult { IsValid = false, ErrorMessage = "Student ID is required" };

            if (request.Amount <= 0)
                return new FeesValidationResult { IsValid = false, ErrorMessage = "Payment amount must be greater than zero" };

            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                return new FeesValidationResult { IsValid = false, ErrorMessage = "Payment method is required" };

            var validMethods = new[] { "Cash", "M-Pesa", "Bank Transfer", "Cheque", "Card Payment" };
            if (!validMethods.Contains(request.PaymentMethod))
                return new FeesValidationResult { IsValid = false, ErrorMessage = "Invalid payment method" };

            return new FeesValidationResult { IsValid = true };
        }

        /// <summary>
        /// Determine if payment should be auto-approved
        /// </summary>
        private bool ShouldAutoApprove(FeesPaymentRequest request)
        {
            // Auto-approve scanned receipts and electronic payments
            return !string.IsNullOrEmpty(request.SlipImageUrl) ||
                   request.PaymentMethod == "M-Pesa" ||
                   !string.IsNullOrEmpty(request.TransactionId);
        }

        /// <summary>
        /// Generate unique receipt number
        /// </summary>
        private string GenerateReceiptNumber()
        {
            return $"RCP{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(100, 999)}";
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
        /// Calculate fee due date based on term
        /// </summary>
        private DateTime CalculateFeeDueDate(int term)
        {
            var year = DateTime.Now.Year;
            return term switch
            {
                1 => new DateTime(year, 3, 31), // End of Term 1
                2 => new DateTime(year, 7, 31), // End of Term 2
                3 => new DateTime(year, 11, 30), // End of Term 3
                _ => DateTime.Now.AddDays(30)
            };
        }

        /// <summary>
        /// Get default fees amount from settings
        /// </summary>
        private async Task<decimal> GetDefaultFeesAmountAsync()
        {
            try
            {
                // In a real implementation, this would come from app settings
                return 80000m; // Default KSh 80,000
            }
            catch
            {
                return 80000m; // Fallback amount
            }
        }

        /// <summary>
        /// Get current student ID from authentication
        /// </summary>
        private async Task<string> GetCurrentStudentIdAsync()
        {
            try
            {
                var userId = await SecureStorage.GetAsync("user_id");
                if (string.IsNullOrEmpty(userId))
                    return null;

                var userType = await SecureStorage.GetAsync("user_type");

                if (userType == "Student")
                {
                    var student = await _supabaseClient
                        .From<StudentEntity>()
                        .Where(x => x.UserId == userId)
                        .Single();
                    return student?.Id;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error getting current student ID");
                return null;
            }
        }

        /// <summary>
        /// Get current user ID
        /// </summary>
        private async Task<string> GetCurrentUserIdAsync()
        {
            try
            {
                return await SecureStorage.GetAsync("user_id") ?? "system";
            }
            catch
            {
                return "system";
            }
        }

        /// <summary>
        /// Get current user name
        /// </summary>
        private async Task<string> GetCurrentUserNameAsync()
        {
            try
            {
                return _authService?.CurrentStudent?.FullName ?? "System";
            }
            catch
            {
                return "System";
            }
        }

        /// <summary>
        /// Check if current user is bursar or admin
        /// </summary>
        private async Task<bool> IsBursarOrAdminAsync()
        {
            try
            {
                var userType = await SecureStorage.GetAsync("user_type");
                return userType == "Bursar" || userType == "Principal" || userType == "Secretary";
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }

    #region Supporting Models

    public class FeesPaymentRequest
    {
        public string StudentId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string? TransactionId { get; set; }
        public string? SlipImageUrl { get; set; }
        public string? Description { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? ReceiptNumber { get; set; }
        public bool RequiresApproval { get; set; }
        public string? PaymentId { get; set; }
    }

    public class FeesValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class FeesSummary
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public decimal TotalFees { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }
        public int DaysOverdue { get; set; }
    }

    public class PendingPayment
    {
        public string PaymentId { get; set; } = string.Empty;
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string ReceiptNumber { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string? SlipImageUrl { get; set; }
        public string? Description { get; set; }
        public string ReceivedBy { get; set; } = string.Empty;
    }

    public class FeesStatement
    {
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Form { get; set; } = string.Empty;
        public string Class { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal TotalFees { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal Balance { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public DateTime GeneratedDate { get; set; }
        public List<PaymentRecord> Payments { get; set; } = new();
    }

    #endregion
}