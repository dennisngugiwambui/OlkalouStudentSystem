// ===============================
// Services/NotificationService.cs - Enhanced Notification Service
// ===============================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OlkalouStudentSystem.Models;

namespace OlkalouStudentSystem.Services
{
    public interface INotificationService
    {
        Task<bool> SendNotificationAsync(string recipientId, string title, string message, string notificationType = "General", string priority = "Normal", string actionUrl = "", DateTime? expiryDate = null);
        Task<ApiResponse<List<Models.Notification>>> GetNotificationsAsync(string userId, int pageSize = 50, int pageNumber = 1);
        Task<ApiResponse<List<Models.Notification>>> GetUnreadNotificationsAsync(string userId);
        Task<ApiResponse<int>> GetUnreadNotificationsCountAsync(string userId);
        Task<bool> MarkAsReadAsync(string notificationId);
        Task<bool> MarkAllAsReadAsync(string userId);
        Task<bool> DeleteNotificationAsync(string notificationId);
        Task<bool> DeleteAllNotificationsAsync(string userId);
        Task<bool> SendBulkNotificationAsync(List<string> recipientIds, string title, string message, string notificationType = "General", string priority = "Normal");
        Task<bool> SendNotificationToUserTypeAsync(UserType userType, string title, string message, string notificationType = "General", string priority = "Normal");
        Task<bool> SendNotificationToFormAsync(string form, string title, string message, string notificationType = "General", string priority = "Normal");
        Task<bool> SendNotificationToClassAsync(string className, string title, string message, string notificationType = "General", string priority = "Normal");
        Task<bool> SendPaymentNotificationAsync(string studentId, decimal amount, string receiptNumber, string paymentMethod);
        Task<bool> SendAssignmentNotificationAsync(string studentId, string assignmentTitle, DateTime dueDate, string subject);
        Task<bool> SendDisciplinaryNotificationAsync(string studentId, string offenseType, string actionTaken);
        Task<bool> SendLibraryNotificationAsync(string userId, string bookTitle, DateTime dueDate, string notificationType);
        Task<bool> SendSalaryApprovalNotificationAsync(string employeeId, decimal amount, string approvalStatus);
        Task<bool> SendMarkEntryNotificationAsync(string studentId, string subject, string term, double marks);
        Task<bool> SendActivityNotificationAsync(List<string> recipientIds, string activityTitle, DateTime activityDate, string venue);
        Task<bool> SendPromotionNotificationAsync(string studentId, string fromForm, string toForm);
        Task<bool> SendRegistrationCompletionNotificationAsync(string userId, string userType, string welcomeMessage);
        Task<bool> SendSubjectSelectionNotificationAsync(string studentId, List<string> selectedSubjects);
        Task<bool> SendContractExpiryNotificationAsync(string teacherId, DateTime expiryDate);
        Task<bool> SendInvoiceApprovalNotificationAsync(string requesterId, string invoiceNumber, string approvalStatus);
        Task CleanupExpiredNotificationsAsync();
    }

    public class NotificationService : INotificationService
    {
        private readonly ApiService _apiService;
        private readonly List<Models.Notification> _notifications;

        public NotificationService(ApiService apiService)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _notifications = new List<Models.Notification>();
        }

        #region Core Notification Methods

        public async Task<bool> SendNotificationAsync(
            string recipientId,
            string title,
            string message,
            string notificationType = "General",
            string priority = "Normal",
            string actionUrl = "",
            DateTime? expiryDate = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(recipientId) || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
                {
                    return false;
                }

                var notification = new Models.Notification
                {
                    NotificationId = Guid.NewGuid().ToString(),
                    Title = title,
                    Message = message,
                    RecipientId = recipientId,
                    NotificationType = notificationType,
                    Priority = priority,
                    ActionUrl = actionUrl,
                    ExpiryDate = expiryDate,
                    CreatedDate = DateTime.Now,
                    IsRead = false,
                    CreatedBy = await GetCurrentUserIdAsync()
                };

                // Add to local collection (in a real app, this would be saved to database)
                _notifications.Add(notification);

                // Send via API
                var response = await _apiService.CreateNotificationAsync(notification);
                
                // Send push notification if needed
                await SendPushNotificationAsync(notification);

                return response?.Success == true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending notification: {ex.Message}");
                return false;
            }
        }

        public async Task<ApiResponse<List<Models.Notification>>> GetNotificationsAsync(string userId, int pageSize = 50, int pageNumber = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return new ApiResponse<List<Models.Notification>>
                    {
                        Success = false,
                        Message = "User ID is required",
                        Data = new List<Models.Notification>()
                    };
                }

                // In a real implementation, this would fetch from API
                var userNotifications = _notifications
                    .Where(n => n.RecipientId == userId || n.RecipientId == "All")
                    .Where(n => !n.ExpiryDate.HasValue || n.ExpiryDate > DateTime.Now)
                    .OrderByDescending(n => n.CreatedDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return new ApiResponse<List<Models.Notification>>
                {
                    Success = true,
                    Message = "Notifications retrieved successfully",
                    Data = userNotifications
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting notifications: {ex.Message}");
                return new ApiResponse<List<Models.Notification>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<Models.Notification>()
                };
            }
        }

        public async Task<ApiResponse<List<Models.Notification>>> GetUnreadNotificationsAsync(string userId)
        {
            try
            {
                var allNotifications = await GetNotificationsAsync(userId);
                if (allNotifications.Success)
                {
                    var unreadNotifications = allNotifications.Data.Where(n => !n.IsRead).ToList();
                    return new ApiResponse<List<Models.Notification>>
                    {
                        Success = true,
                        Message = "Unread notifications retrieved successfully",
                        Data = unreadNotifications
                    };
                }

                return allNotifications;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting unread notifications: {ex.Message}");
                return new ApiResponse<List<Models.Notification>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = new List<Models.Notification>()
                };
            }
        }

        public async Task<ApiResponse<int>> GetUnreadNotificationsCountAsync(string userId)
        {
            try
            {
                var unreadNotifications = await GetUnreadNotificationsAsync(userId);
                return new ApiResponse<int>
                {
                    Success = unreadNotifications.Success,
                    Message = unreadNotifications.Message,
                    Data = unreadNotifications.Success ? unreadNotifications.Data.Count : 0
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting unread notifications count: {ex.Message}");
                return new ApiResponse<int>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = 0
                };
            }
        }

        public async Task<bool> MarkAsReadAsync(string notificationId)
        {
            try
            {
                var notification = _notifications.FirstOrDefault(n => n.NotificationId == notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    // In real implementation, update in database via API
                    await _apiService.UpdateNotificationAsync(notification);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking notification as read: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(string userId)
        {
            try
            {
                var userNotifications = _notifications.Where(n => n.RecipientId == userId && !n.IsRead).ToList();
                foreach (var notification in userNotifications)
                {
                    notification.IsRead = true;
                }

                // In real implementation, bulk update via API
                await _apiService.MarkAllNotificationsAsReadAsync(userId);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking all notifications as read: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(string notificationId)
        {
            try
            {
                var notification = _notifications.FirstOrDefault(n => n.NotificationId == notificationId);
                if (notification != null)
                {
                    _notifications.Remove(notification);
                    await _apiService.DeleteNotificationAsync(notificationId);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting notification: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAllNotificationsAsync(string userId)
        {
            try
            {
                var userNotifications = _notifications.Where(n => n.RecipientId == userId).ToList();
                foreach (var notification in userNotifications)
                {
                    _notifications.Remove(notification);
                }

                await _apiService.DeleteAllNotificationsAsync(userId);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting all notifications: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Bulk Notification Methods

        public async Task<bool> SendBulkNotificationAsync(
            List<string> recipientIds,
            string title,
            string message,
            string notificationType = "General",
            string priority = "Normal")
        {
            try
            {
                if (recipientIds == null || !recipientIds.Any())
                {
                    return false;
                }

                var tasks = recipientIds.Select(recipientId =>
                    SendNotificationAsync(recipientId, title, message, notificationType, priority));

                var results = await Task.WhenAll(tasks);
                return results.All(result => result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending bulk notifications: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendNotificationToUserTypeAsync(
            UserType userType,
            string title,
            string message,
            string notificationType = "General",
            string priority = "Normal")
        {
            try
            {
                // Get all users of specified type
                var users = await GetUsersByTypeAsync(userType);
                var recipientIds = users.Select(u => u.UserId).ToList();

                return await SendBulkNotificationAsync(recipientIds, title, message, notificationType, priority);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending notification to user type: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendNotificationToFormAsync(
            string form,
            string title,
            string message,
            string notificationType = "General",
            string priority = "Normal")
        {
            try
            {
                // Get all students in specified form
                var students = await GetStudentsByFormAsync(form);
                var recipientIds = students.Select(s => s.StudentId).ToList();

                return await SendBulkNotificationAsync(recipientIds, title, message, notificationType, priority);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending notification to form: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendNotificationToClassAsync(
            string className,
            string title,
            string message,
            string notificationType = "General",
            string priority = "Normal")
        {
            try
            {
                // Get all students in specified class
                var students = await GetStudentsByClassAsync(className);
                var recipientIds = students.Select(s => s.StudentId).ToList();

                return await SendBulkNotificationAsync(recipientIds, title, message, notificationType, priority);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending notification to class: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Specialized Notification Methods

        public async Task<bool> SendPaymentNotificationAsync(string studentId, decimal amount, string receiptNumber, string paymentMethod)
        {
            var title = "Payment Received";
            var message = $"Your payment of KSh {amount:N2} has been received successfully. Receipt Number: {receiptNumber}. Payment Method: {paymentMethod}";
            
            return await SendNotificationAsync(studentId, title, message, "Payment", "Normal", "/fees");
        }

        public async Task<bool> SendAssignmentNotificationAsync(string studentId, string assignmentTitle, DateTime dueDate, string subject)
        {
            var title = "New Assignment";
            var message = $"New assignment '{assignmentTitle}' for {subject} is due on {dueDate:MMM dd, yyyy}";
            
            return await SendNotificationAsync(studentId, title, message, "Assignment", "Normal", "/assignments");
        }

        public async Task<bool> SendDisciplinaryNotificationAsync(string studentId, string offenseType, string actionTaken)
        {
            var title = "Disciplinary Action";
            var message = $"Disciplinary action recorded for {offenseType}. Action taken: {actionTaken}";
            
            return await SendNotificationAsync(studentId, title, message, "Disciplinary", "High", "/disciplinary");
        }

        public async Task<bool> SendLibraryNotificationAsync(string userId, string bookTitle, DateTime dueDate, string notificationType)
        {
            var title = notificationType switch
            {
                "BookIssued" => "Book Issued",
                "BookDue" => "Book Due Soon",
                "BookOverdue" => "Book Overdue",
                "BookReturned" => "Book Returned",
                _ => "Library Notification"
            };

            var message = notificationType switch
            {
                "BookIssued" => $"You have successfully borrowed '{bookTitle}'. Due date: {dueDate:MMM dd, yyyy}",
                "BookDue" => $"Book '{bookTitle}' is due on {dueDate:MMM dd, yyyy}",
                "BookOverdue" => $"Book '{bookTitle}' was due on {dueDate:MMM dd, yyyy} and is now overdue",
                "BookReturned" => $"You have successfully returned '{bookTitle}'",
                _ => $"Library notification for '{bookTitle}'"
            };

            var priority = notificationType == "BookOverdue" ? "High" : "Normal";
            
            return await SendNotificationAsync(userId, title, message, "Library", priority, "/library");
        }

        public async Task<bool> SendSalaryApprovalNotificationAsync(string employeeId, decimal amount, string approvalStatus)
        {
            var title = approvalStatus == "Approved" ? "Salary Approved" : "Salary Pending";
            var message = approvalStatus == "Approved"
                ? $"Your salary of KSh {amount:N2} has been approved and will be processed soon"
                : $"Your salary of KSh {amount:N2} is pending approval";
            
            return await SendNotificationAsync(employeeId, title, message, "Salary", "Normal", "/salary");
        }

        public async Task<bool> SendMarkEntryNotificationAsync(string studentId, string subject, string term, double marks)
        {
            var title = "Marks Entered";
            var message = $"Your marks for {subject} in {term} have been entered: {marks}%";
            
            return await SendNotificationAsync(studentId, title, message, "Academic", "Normal", "/performance");
        }

        public async Task<bool> SendActivityNotificationAsync(List<string> recipientIds, string activityTitle, DateTime activityDate, string venue)
        {
            var title = "School Activity";
            var message = $"'{activityTitle}' scheduled for {activityDate:MMM dd, yyyy} at {venue}";
            
            return await SendBulkNotificationAsync(recipientIds, title, message, "Activity", "Normal");
        }

        public async Task<bool> SendPromotionNotificationAsync(string studentId, string fromForm, string toForm)
        {
            var title = "Class Promotion";
            var message = $"Congratulations! You have been promoted from {fromForm} to {toForm}";
            
            return await SendNotificationAsync(studentId, title, message, "Academic", "Normal");
        }

        public async Task<bool> SendRegistrationCompletionNotificationAsync(string userId, string userType, string welcomeMessage)
        {
            var title = "Registration Complete";
            var message = $"Welcome to Grace Secondary School! {welcomeMessage}";
            
            return await SendNotificationAsync(userId, title, message, "Registration", "Normal");
        }

        public async Task<bool> SendSubjectSelectionNotificationAsync(string studentId, List<string> selectedSubjects)
        {
            var title = "Subject Selection Confirmed";
            var subjects = string.Join(", ", selectedSubjects);
            var message = $"Your subject selection has been confirmed: {subjects}";
            
            return await SendNotificationAsync(studentId, title, message, "Academic", "Normal", "/subjects");
        }

        public async Task<bool> SendContractExpiryNotificationAsync(string teacherId, DateTime expiryDate)
        {
            var title = "Contract Expiry Notice";
            var daysRemaining = (expiryDate - DateTime.Now).Days;
            var message = $"Your contract expires on {expiryDate:MMM dd, yyyy} ({daysRemaining} days remaining)";
            var priority = daysRemaining <= 30 ? "High" : "Normal";
            
            return await SendNotificationAsync(teacherId, title, message, "Contract", priority);
        }

        public async Task<bool> SendInvoiceApprovalNotificationAsync(string requesterId, string invoiceNumber, string approvalStatus)
        {
            var title = approvalStatus == "Approved" ? "Invoice Approved" : "Invoice Rejected";
            var message = $"Invoice {invoiceNumber} has been {approvalStatus.ToLower()}";
            
            return await SendNotificationAsync(requesterId, title, message, "Financial", "Normal", "/invoices");
        }

        #endregion

        #region Cleanup and Maintenance

        public async Task CleanupExpiredNotificationsAsync()
        {
            try
            {
                var expiredNotifications = _notifications
                    .Where(n => n.ExpiryDate.HasValue && n.ExpiryDate < DateTime.Now)
                    .ToList();

                foreach (var notification in expiredNotifications)
                {
                    _notifications.Remove(notification);
                }

                // In real implementation, delete from database
                await _apiService.CleanupExpiredNotificationsAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cleaning up expired notifications: {ex.Message}");
            }
        }

        #endregion

        #region Helper Methods

        private async Task<string> GetCurrentUserIdAsync()
        {
            try
            {
                return await SecureStorage.GetAsync("user_id") ?? "System";
            }
            catch
            {
                return "System";
            }
        }

        private async Task<List<UserProfile>> GetUsersByTypeAsync(UserType userType)
        {
            try
            {
                // In real implementation, this would call API
                return await _apiService.GetUsersByTypeAsync(userType) ?? new List<UserProfile>();
            }
            catch
            {
                return new List<UserProfile>();
            }
        }

        private async Task<List<Models.Student>> GetStudentsByFormAsync(string form)
        {
            try
            {
                return await _apiService.GetStudentsByFormAsync(form) ?? new List<Models.Student>();
            }
            catch
            {
                return new List<Models.Student>();
            }
        }

        private async Task<List<Models.Student>> GetStudentsByClassAsync(string className)
        {
            try
            {
                return await _apiService.GetStudentsByClassAsync(className) ?? new List<Models.Student>();
            }
            catch
            {
                return new List<Models.Student>();
            }
        }

        private async Task SendPushNotificationAsync(Models.Notification notification)
        {
            try
            {
                // In real implementation, integrate with push notification service
                // For now, just log the notification
                System.Diagnostics.Debug.WriteLine($"Push notification sent: {notification.Title} - {notification.Message}");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error sending push notification: {ex.Message}");
            }
        }

        #endregion
    }
   
}