// ===============================
// ViewModels/FeesViewModel.cs - Enhanced Fees Management ViewModel
// ===============================
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.Models;
using OlkalouStudentSystem.Services;

namespace OlkalouStudentSystem.ViewModels
{
    public class FeesViewModel : BaseViewModel
    {
        private readonly Services.ApiService _apiService;
        private readonly Services.AuthService _authService;
        private readonly Services.FeesService _feesService;
        private readonly Services.NotificationService _notificationService;
        private readonly Services.FileService _fileService;
        private readonly ILogger<FeesViewModel>? _logger;

        #region Private Fields
        private ObservableCollection<Models.FeesInfo> _feesInfo;
        private ObservableCollection<Models.PaymentRecord> _paymentHistory;
        private ObservableCollection<Models.Invoice> _invoices;
        private ObservableCollection<Models.SalaryPayment> _salaryPayments;
        private ObservableCollection<Models.JournalEntry> _journalEntries;
        private ObservableCollection<Models.FeesStructure> _feesStructures;
        private ObservableCollection<Models.Student> _studentsWithPendingFees;
        private ObservableCollection<Models.PaymentRecord> _pendingPayments;

        private string _userType = "Student";
        private string _currentUserId;
        private decimal _paymentAmount;
        private string _paymentMethod = "M-Pesa";
        private string _selectedReceiptImagePath;
        private bool _isBursar;
        private bool _isPrincipal;
        private bool _isStudent;
        private Models.FeesInfo _selectedStudentFees;
        private Models.PaymentRecord _selectedPayment;
        private Models.SalaryPayment _selectedSalaryPayment;
        private Models.Invoice _selectedInvoice;
        private string _receiptNumber;
        private DateTime _paymentDate = DateTime.Now;
        private string _paymentDescription;
        private decimal _totalFeesCollected;
        private decimal _totalPendingFees;
        private int _studentsWithBalances;
        #endregion

        #region Constructor
        public FeesViewModel(
            Services.ApiService apiService,
            Services.AuthService authService,
            Services.FeesService feesService,
            Services.NotificationService notificationService,
            Services.FileService fileService,
            ILogger<FeesViewModel>? logger = null)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _feesService = feesService ?? throw new ArgumentNullException(nameof(feesService));
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _logger = logger;

            Title = "Fees Management";

            // Initialize collections
            FeesInfo = new ObservableCollection<Models.FeesInfo>();
            PaymentHistory = new ObservableCollection<Models.PaymentRecord>();
            Invoices = new ObservableCollection<Models.Invoice>();
            SalaryPayments = new ObservableCollection<Models.SalaryPayment>();
            JournalEntries = new ObservableCollection<Models.JournalEntry>();
            FeesStructures = new ObservableCollection<Models.FeesStructure>();
            StudentsWithPendingFees = new ObservableCollection<Models.Student>();
            PendingPayments = new ObservableCollection<Models.PaymentRecord>();

            InitializeCommands();
            LoadUserTypeAsync();
        }
        #endregion

        #region Properties
        public ObservableCollection<Models.FeesInfo> FeesInfo
        {
            get => _feesInfo;
            set => SetProperty(ref _feesInfo, value);
        }

        public ObservableCollection<Models.PaymentRecord> PaymentHistory
        {
            get => _paymentHistory;
            set => SetProperty(ref _paymentHistory, value);
        }

        public ObservableCollection<Models.Invoice> Invoices
        {
            get => _invoices;
            set => SetProperty(ref _invoices, value);
        }

        public ObservableCollection<Models.SalaryPayment> SalaryPayments
        {
            get => _salaryPayments;
            set => SetProperty(ref _salaryPayments, value);
        }

        public ObservableCollection<Models.JournalEntry> JournalEntries
        {
            get => _journalEntries;
            set => SetProperty(ref _journalEntries, value);
        }

        public ObservableCollection<Models.FeesStructure> FeesStructures
        {
            get => _feesStructures;
            set => SetProperty(ref _feesStructures, value);
        }

        public ObservableCollection<Models.Student> StudentsWithPendingFees
        {
            get => _studentsWithPendingFees;
            set => SetProperty(ref _studentsWithPendingFees, value);
        }

        public ObservableCollection<Models.PaymentRecord> PendingPayments
        {
            get => _pendingPayments;
            set => SetProperty(ref _pendingPayments, value);
        }

        public string UserType
        {
            get => _userType;
            set
            {
                SetProperty(ref _userType, value);
                UpdateUserTypeFlags();
                UpdateTitle();
                UpdateCommandAvailability();
            }
        }

        public decimal PaymentAmount
        {
            get => _paymentAmount;
            set => SetProperty(ref _paymentAmount, value);
        }

        public string PaymentMethod
        {
            get => _paymentMethod;
            set => SetProperty(ref _paymentMethod, value);
        }

        public string SelectedReceiptImagePath
        {
            get => _selectedReceiptImagePath;
            set => SetProperty(ref _selectedReceiptImagePath, value);
        }

        public bool IsBursar
        {
            get => _isBursar;
            set => SetProperty(ref _isBursar, value);
        }

        public bool IsPrincipal
        {
            get => _isPrincipal;
            set => SetProperty(ref _isPrincipal, value);
        }

        public bool IsStudent
        {
            get => _isStudent;
            set => SetProperty(ref _isStudent, value);
        }

        public Models.FeesInfo SelectedStudentFees
        {
            get => _selectedStudentFees;
            set => SetProperty(ref _selectedStudentFees, value);
        }

        public Models.PaymentRecord SelectedPayment
        {
            get => _selectedPayment;
            set => SetProperty(ref _selectedPayment, value);
        }

        public Models.SalaryPayment SelectedSalaryPayment
        {
            get => _selectedSalaryPayment;
            set => SetProperty(ref _selectedSalaryPayment, value);
        }

        public Models.Invoice SelectedInvoice
        {
            get => _selectedInvoice;
            set => SetProperty(ref _selectedInvoice, value);
        }

        public string ReceiptNumber
        {
            get => _receiptNumber;
            set => SetProperty(ref _receiptNumber, value);
        }

        public DateTime PaymentDate
        {
            get => _paymentDate;
            set => SetProperty(ref _paymentDate, value);
        }

        public string PaymentDescription
        {
            get => _paymentDescription;
            set => SetProperty(ref _paymentDescription, value);
        }

        public decimal TotalFeesCollected
        {
            get => _totalFeesCollected;
            set => SetProperty(ref _totalFeesCollected, value);
        }

        public decimal TotalPendingFees
        {
            get => _totalPendingFees;
            set => SetProperty(ref _totalPendingFees, value);
        }

        public int StudentsWithBalances
        {
            get => _studentsWithBalances;
            set => SetProperty(ref _studentsWithBalances, value);
        }

        // Permission Properties
        public bool CanMakePayments => IsStudent;
        public bool CanApprovePayments => IsBursar || IsPrincipal;
        public bool CanManageInvoices => IsBursar;
        public bool CanApproveInvoices => IsPrincipal;
        public bool CanManageJournalEntries => IsBursar;
        public bool CanProcessSalaries => IsBursar;
        public bool CanApproveSalaries => IsPrincipal;
        public bool CanViewAllFees => IsBursar || IsPrincipal;
        public bool CanUploadReceipts => IsBursar;

        // Available payment methods
        public List<string> PaymentMethods => new List<string>
        {
            "M-Pesa", "Bank Transfer", "Cash", "Cheque", "Bank Deposit"
        };
        #endregion

        #region Commands
        public ICommand LoadFeesCommand { get; private set; }
        public ICommand MakePaymentCommand { get; private set; }
        public ICommand ApprovePaymentCommand { get; private set; }
        public ICommand RejectPaymentCommand { get; private set; }
        public ICommand CreateInvoiceCommand { get; private set; }
        public ICommand ApproveInvoiceCommand { get; private set; }
        public ICommand RejectInvoiceCommand { get; private set; }
        public ICommand CreateJournalEntryCommand { get; private set; }
        public ICommand GenerateTrialBalanceCommand { get; private set; }
        public ICommand ProcessSalaryPaymentCommand { get; private set; }
        public ICommand ApproveSalaryPaymentCommand { get; private set; }
        public ICommand UploadReceiptCommand { get; private set; }
        public ICommand ViewPaymentDetailsCommand { get; private set; }
        public ICommand PrintReceiptCommand { get; private set; }
        public ICommand ExportFeesReportCommand { get; private set; }
        public ICommand SendPaymentReminderCommand { get; private set; }
        public ICommand ViewStudentFeesCommand { get; private set; }
        public ICommand UpdateFeesStructureCommand { get; private set; }
        public ICommand GenerateFinancialReportCommand { get; private set; }
        public ICommand RefreshDataCommand { get; private set; }
        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            LoadFeesCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(LoadFeesAsync);
            MakePaymentCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(MakePaymentAsync, () => CanMakePayments);
            ApprovePaymentCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.PaymentRecord>(ApprovePaymentAsync, (payment) => CanApprovePayments);
            RejectPaymentCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.PaymentRecord>(RejectPaymentAsync, (payment) => CanApprovePayments);
            CreateInvoiceCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(CreateInvoiceAsync, () => CanManageInvoices);
            ApproveInvoiceCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.Invoice>(ApproveInvoiceAsync, (invoice) => CanApproveInvoices);
            RejectInvoiceCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.Invoice>(RejectInvoiceAsync, (invoice) => CanApproveInvoices);
            CreateJournalEntryCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(CreateJournalEntryAsync, () => CanManageJournalEntries);
            GenerateTrialBalanceCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(GenerateTrialBalanceAsync, () => CanManageJournalEntries);
            ProcessSalaryPaymentCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.SalaryPayment>(ProcessSalaryPaymentAsync, (payment) => CanProcessSalaries);
            ApproveSalaryPaymentCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.SalaryPayment>(ApproveSalaryPaymentAsync, (payment) => CanApproveSalaries);
            UploadReceiptCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(UploadReceiptAsync, () => CanUploadReceipts);
            ViewPaymentDetailsCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.PaymentRecord>(ViewPaymentDetailsAsync);
            PrintReceiptCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.PaymentRecord>(PrintReceiptAsync);
            ExportFeesReportCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(ExportFeesReportAsync, () => CanViewAllFees);
            SendPaymentReminderCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.Student>(SendPaymentReminderAsync, (student) => CanViewAllFees);
            ViewStudentFeesCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.Student>(ViewStudentFeesAsync);
            UpdateFeesStructureCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(UpdateFeesStructureAsync, () => IsPrincipal);
            GenerateFinancialReportCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(GenerateFinancialReportAsync, () => CanViewAllFees);
            RefreshDataCommand = new Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand(RefreshDataAsync);
        }

        private async void LoadUserTypeAsync()
        {
            try
            {
                UserType = await GetSecureStorageAsync("user_type") ?? "Student";
                _currentUserId = await GetSecureStorageAsync("user_id") ?? "default_user";
                UpdateUserTypeFlags();
                UpdateTitle();
                UpdateCommandAvailability();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading user type");
            }
        }

        private void UpdateUserTypeFlags()
        {
            IsBursar = UserType == "Bursar";
            IsPrincipal = UserType == "Principal";
            IsStudent = UserType == "Student";
        }

        private void UpdateCommandAvailability()
        {
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand)MakePaymentCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.PaymentRecord>)ApprovePaymentCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.PaymentRecord>)RejectPaymentCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand)CreateInvoiceCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.Invoice>)ApproveInvoiceCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.Invoice>)RejectInvoiceCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand)CreateJournalEntryCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand)GenerateTrialBalanceCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.SalaryPayment>)ProcessSalaryPaymentCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.SalaryPayment>)ApproveSalaryPaymentCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand)UploadReceiptCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand)ExportFeesReportCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand<Models.Student>)SendPaymentReminderCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand)UpdateFeesStructureCommand)?.NotifyCanExecuteChanged();
            ((Microsoft.Toolkit.Mvvm.Input.AsyncRelayCommand)GenerateFinancialReportCommand)?.NotifyCanExecuteChanged();
        }

        private void UpdateTitle()
        {
            Title = UserType switch
            {
                "Student" => "My Fees",
                "Bursar" => "Financial Management",
                "Principal" => "Financial Oversight",
                _ => "Fees Management"
            };
        }
        #endregion

        #region Public Methods
        public override async Task InitializeAsync()
        {
            await LoadFeesAsync();

            if (IsBursar)
            {
                await LoadInvoicesAsync();
                await LoadSalaryPaymentsAsync();
                await LoadJournalEntriesAsync();
                await LoadPendingPaymentsAsync();
                await LoadStudentsWithPendingFeesAsync();
                await CalculateFinancialSummaryAsync();
            }
            else if (IsPrincipal)
            {
                await LoadPendingApprovalsAsync();
                await CalculateFinancialSummaryAsync();
            }
        }
        #endregion

        #region Data Loading Methods
        private async Task LoadFeesAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
            try
            {
                if (IsStudent)
                {
                    // Load student's own fees
                    var fees = await _feesService.GetCurrentStudentFeesAsync();
                    if (fees != null)
                    {
                        FeesInfo.Clear();
                        FeesInfo.Add(fees);

                        PaymentHistory.Clear();
                        foreach (var payment in fees.PaymentHistory)
                        {
                            PaymentHistory.Add(payment);
                        }
                    }
                }
                else if (CanViewAllFees)
                {
                    // Load all students' fees for bursar/principal
                    var allFees = await _feesService.GetAllStudentsFeesAsync();
                    FeesInfo.Clear();
                    foreach (var fee in allFees)
                    {
                        var feesInfo = new Models.FeesInfo
                        {
                            StudentId = fee.StudentId,
                            TotalFees = fee.TotalFees,
                            PaidAmount = fee.PaidAmount,
                            Balance = fee.Balance,
                            PaymentStatus = fee.PaymentStatus,
                            DueDate = fee.DueDate,
                            PaymentHistory = new List<Models.PaymentRecord>()
                        };
                        FeesInfo.Add(feesInfo);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to load fees: {ex.Message}");
            }
        #endregion

            #region Salary Management Methods
        private async Task ProcessSalaryPaymentAsync(Models.SalaryPayment payment)
        {
            if (payment == null) return;

            try
            {
                var confirm = await ShowConfirmAsync("Process Salary",
                    $"Process salary payment for {payment.EmployeeName} - KSh {payment.NetSalary:N2}?");

                if (confirm)
                {
                    IsBusy = true;

                    payment.Status = "Paid";
                    payment.PaidBy = _currentUserId;
                    payment.PaymentDate = DateTime.Now;

                    var success = await _apiService.UpdateSalaryPaymentAsync(payment);
                    if (success)
                    {
                        await _notificationService.SendNotificationAsync(
                            payment.EmployeeId,
                            "Salary Paid",
                            $"Your salary of KSh {payment.NetSalary:N2} for {payment.PayrollMonth} {payment.PayrollYear} has been processed.");

                        await ShowSuccessAsync("Success", "Salary payment processed successfully.");
                        await LoadSalaryPaymentsAsync();
                    }
                    else
                    {
                        await ShowErrorAsync("Error", "Failed to process salary payment.");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to process salary payment: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ApproveSalaryPaymentAsync(Models.SalaryPayment payment)
        {
            if (payment == null) return;

            try
            {
                var confirm = await ShowConfirmAsync("Approve Salary",
                    $"Approve salary for {payment.EmployeeName} - KSh {payment.NetSalary:N2}?");

                if (confirm)
                {
                    IsBusy = true;

                    payment.Status = "Approved";
                    payment.ApprovedBy = _currentUserId;
                    payment.ApprovalDate = DateTime.Now;

                    var success = await _apiService.UpdateSalaryPaymentAsync(payment);
                    if (success)
                    {
                        await _notificationService.SendSalaryApprovalNotificationAsync(
                            payment.EmployeeId,
                            payment.NetSalary,
                            "Approved");

                        // Notify bursar to process payment
                        await _notificationService.SendNotificationToUserTypeAsync(
                            "Bursar",
                            "Salary Approved",
                            $"Salary for {payment.EmployeeName} has been approved and is ready for processing.");

                        await ShowSuccessAsync("Success", "Salary approved successfully.");
                        await LoadSalaryPaymentsAsync();
                    }
                    else
                    {
                        await ShowErrorAsync("Error", "Failed to approve salary.");
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to approve salary: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region Journal Entry and Accounting Methods
        private async Task CreateJournalEntryAsync()
        {
            try
            {
                await NavigateToAsync("//create-journal-entry");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to navigate to journal entry creation: {ex.Message}");
            }
        }

        private async Task GenerateTrialBalanceAsync()
        {
            if (!CanManageJournalEntries) return;

            try
            {
                IsBusy = true;

                // Simulate trial balance generation
                await Task.Delay(3000);

                await ShowSuccessAsync("Success", "Trial balance generated successfully. Check your reports section.");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to generate trial balance: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region File Upload Methods
        private async Task UploadReceiptAsync()
        {
            try
            {
                var fileData = await _fileService.PickImageAsync();
                if (fileData != null)
                {
                    // Validate image quality (HDR)
                    if (fileData.Quality != "HDR" && fileData.Size < 1024 * 1024) // Less than 1MB
                    {
                        var proceed = await ShowConfirmAsync("Image Quality",
                            "The selected image may not be high quality. Proceed anyway?");
                        if (!proceed) return;
                    }

                    IsBusy = true;

                    var uploadResult = await _fileService.UploadFileAsync(fileData);
                    if (uploadResult.Success)
                    {
                        SelectedReceiptImagePath = uploadResult.FilePath;
                        await ShowSuccessAsync("Success", "Receipt uploaded successfully.");
                    }
                    else
                    {
                        await ShowErrorAsync("Upload Failed", uploadResult.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to upload receipt: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region Reporting and Export Methods
        private async Task ViewPaymentDetailsAsync(Models.PaymentRecord payment)
        {
            if (payment == null) return;

            var details = $"Payment ID: {payment.PaymentId}\n" +
                         $"Amount: KSh {payment.Amount:N2}\n" +
                         $"Date: {payment.PaymentDate:MMM dd, yyyy}\n" +
                         $"Method: {payment.PaymentMethod}\n" +
                         $"Receipt: {payment.ReceiptNumber}\n" +
                         $"Status: {payment.Status}\n" +
                         $"Description: {payment.Description}";

            await ShowInfoAsync("Payment Details", details);
        }

        private async Task PrintReceiptAsync(Models.PaymentRecord payment)
        {
            if (payment == null || !payment.CanDownloadReceipt) return;

            try
            {
                IsBusy = true;

                // Simulate receipt generation and printing
                await Task.Delay(2000);

                await ShowSuccessAsync("Success", $"Receipt {payment.ReceiptNumber} has been sent to printer.");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to print receipt: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExportFeesReportAsync()
        {
            if (!CanViewAllFees) return;

            try
            {
                IsBusy = true;

                // Simulate report generation
                await Task.Delay(3000);

                await ShowSuccessAsync("Export Complete", "Fees report exported successfully to your device.");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to export report: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task GenerateFinancialReportAsync()
        {
            if (!CanViewAllFees) return;

            try
            {
                IsBusy = true;

                // Simulate financial report generation
                await Task.Delay(4000);

                await ShowSuccessAsync("Report Generated", "Financial report generated successfully.");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to generate financial report: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
        #endregion

        #region Student Management Methods
        private async Task SendPaymentReminderAsync(Models.Student student)
        {
            if (student == null || !CanViewAllFees) return;

            try
            {
                var studentFees = FeesInfo.FirstOrDefault(f => f.StudentId == student.StudentId);
                if (studentFees != null && studentFees.Balance > 0)
                {
                    await _notificationService.SendNotificationAsync(
                        student.StudentId,
                        "Payment Reminder",
                        $"Dear {student.FullName}, your school fees balance is KSh {studentFees.Balance:N2}. Please make payment to avoid inconvenience.");

                    await ShowSuccessAsync("Reminder Sent", $"Payment reminder sent to {student.FullName}.");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to send payment reminder: {ex.Message}");
            }
        }

        private async Task ViewStudentFeesAsync(Models.Student student)
        {
            if (student == null) return;

            try
            {
                await NavigateToAsync($"//student-fees-details?studentId={student.StudentId}");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to view student fees: {ex.Message}");
            }
        }

        private async Task UpdateFeesStructureAsync()
        {
            if (!IsPrincipal) return;

            try
            {
                await NavigateToAsync("//fees-structure");
            }
            catch (Exception ex)
            {
                await ShowErrorAsync("Error", $"Failed to navigate to fees structure: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods
        private async Task ShowInfoAsync(string title, string message)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }
        #endregion
    }

    #region Command Implementation
    public class Command : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public Command(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public void ChangeCanExecute() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    public class Command<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool> _canExecute;

        public Command(Action<T> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter) => _canExecute?.Invoke((T)parameter) ?? true;

        public void Execute(object parameter) => _execute((T)parameter);

        public void ChangeCanExecute() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
    #endregion
}
        }

        private async Task LoadInvoicesAsync()
{
    try
    {
        var invoices = await _apiService.GetInvoicesAsync();
        Invoices.Clear();
        foreach (var invoice in invoices)
        {
            Invoices.Add(invoice);
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error loading invoices");
    }
}

private async Task LoadSalaryPaymentsAsync()
{
    try
    {
        var salaryPayments = await _apiService.GetSalaryPaymentsAsync();
        SalaryPayments.Clear();
        foreach (var payment in salaryPayments)
        {
            SalaryPayments.Add(payment);
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error loading salary payments");
    }
}

private async Task LoadJournalEntriesAsync()
{
    try
    {
        var entries = await _apiService.GetJournalEntriesAsync();
        JournalEntries.Clear();
        foreach (var entry in entries)
        {
            JournalEntries.Add(entry);
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error loading journal entries");
    }
}

private async Task LoadPendingPaymentsAsync()
{
    try
    {
        var pendingPayments = await _feesService.GetPendingPaymentsAsync();
        PendingPayments.Clear();
        foreach (var payment in pendingPayments)
        {
            var paymentRecord = new Models.PaymentRecord
            {
                PaymentId = payment.PaymentId,
                StudentId = payment.StudentId,
                Amount = payment.Amount,
                PaymentMethod = payment.PaymentMethod,
                PaymentDate = payment.PaymentDate,
                ReceiptNumber = payment.ReceiptNumber,
                Description = payment.Description,
                ReceivedBy = payment.ReceivedBy,
                IsApproved = false,
                Status = "Pending"
            };
            PendingPayments.Add(paymentRecord);
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error loading pending payments");
    }
}

private async Task LoadStudentsWithPendingFeesAsync()
{
    try
    {
        var students = await _apiService.GetStudentsWithPendingFeesAsync();
        StudentsWithPendingFees.Clear();
        foreach (var student in students)
        {
            StudentsWithPendingFees.Add(student);
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error loading students with pending fees");
    }
}

private async Task LoadPendingApprovalsAsync()
{
    try
    {
        // Load pending invoices for approval
        var pendingInvoices = await _apiService.GetPendingInvoicesAsync();
        Invoices.Clear();
        foreach (var invoice in pendingInvoices.Where(i => i.Status == "Pending"))
        {
            Invoices.Add(invoice);
        }

        // Load pending salary payments for approval
        var pendingSalaries = await _apiService.GetPendingSalaryPaymentsAsync();
        SalaryPayments.Clear();
        foreach (var salary in pendingSalaries.Where(s => s.Status == "Pending"))
        {
            SalaryPayments.Add(salary);
        }
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error loading pending approvals");
    }
}

private async Task CalculateFinancialSummaryAsync()
{
    try
    {
        TotalFeesCollected = FeesInfo.Sum(f => f.PaidAmount);
        TotalPendingFees = FeesInfo.Sum(f => f.Balance);
        StudentsWithBalances = FeesInfo.Count(f => f.Balance > 0);
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Error calculating financial summary");
    }
}

private async Task RefreshDataAsync()
{
    await LoadFeesAsync();
    if (IsBursar)
    {
        await LoadInvoicesAsync();
        await LoadSalaryPaymentsAsync();
        await LoadJournalEntriesAsync();
        await LoadPendingPaymentsAsync();
        await LoadStudentsWithPendingFeesAsync();
        await CalculateFinancialSummaryAsync();
    }
    else if (IsPrincipal)
    {
        await LoadPendingApprovalsAsync();
        await CalculateFinancialSummaryAsync();
    }
}
#endregion

#region Payment Processing Methods
private async Task MakePaymentAsync()
{
    if (PaymentAmount <= 0)
    {
        await ShowErrorAsync("Invalid Amount", "Please enter a valid payment amount.");
        return;
    }

    try
    {
        IsBusy = true;

        var paymentRequest = new FeesPaymentRequest
        {
            StudentId = _currentUserId,
            Amount = PaymentAmount,
            PaymentMethod = PaymentMethod,
            Description = PaymentDescription ?? "School fees payment",
            SlipImageUrl = SelectedReceiptImagePath
        };

        var result = await _feesService.ProcessPaymentAsync(paymentRequest);

        if (result.Success)
        {
            // Generate receipt number
            ReceiptNumber = result.ReceiptNumber;

            // Send notification
            await _notificationService.SendPaymentNotificationAsync(
                _currentUserId,
                PaymentAmount,
                ReceiptNumber,
                PaymentMethod);

            await ShowSuccessAsync("Payment Successful",
                $"Payment of KSh {PaymentAmount:N2} received successfully. Receipt: {ReceiptNumber}");

            // Clear payment form
            PaymentAmount = 0;
            PaymentDescription = "";
            SelectedReceiptImagePath = "";

            // Refresh fees data
            await LoadFeesAsync();
        }
        else
        {
            await ShowErrorAsync("Payment Failed", result.Message);
        }
    }
    catch (Exception ex)
    {
        await ShowErrorAsync("Error", $"Failed to process payment: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

private async Task ApprovePaymentAsync(Models.PaymentRecord payment)
{
    if (payment == null) return;

    try
    {
        var confirm = await ShowConfirmAsync("Approve Payment",
            $"Approve payment of KSh {payment.Amount:N2} from {payment.StudentId}?");

        if (confirm)
        {
            IsBusy = true;

            var success = await _feesService.ApprovePaymentAsync(payment.PaymentId);
            if (success)
            {
                payment.IsApproved = true;
                payment.Status = "Approved";

                await _notificationService.SendNotificationAsync(
                    payment.StudentId,
                    "Payment Approved",
                    $"Your payment of KSh {payment.Amount:N2} has been approved.");

                await ShowSuccessAsync("Success", "Payment approved successfully.");
                await LoadFeesAsync();
            }
            else
            {
                await ShowErrorAsync("Error", "Failed to approve payment.");
            }
        }
    }
    catch (Exception ex)
    {
        await ShowErrorAsync("Error", $"Failed to approve payment: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

private async Task RejectPaymentAsync(Models.PaymentRecord payment)
{
    if (payment == null) return;

    try
    {
        var reason = await ShowPromptAsync("Reject Payment",
            "Please provide a reason for rejection:", "Reject", "Cancel");

        if (!string.IsNullOrEmpty(reason))
        {
            IsBusy = true;

            var success = await _feesService.RejectPaymentAsync(payment.PaymentId, reason);
            if (success)
            {
                payment.Status = "Rejected";

                await _notificationService.SendNotificationAsync(
                    payment.StudentId,
                    "Payment Rejected",
                    $"Your payment of KSh {payment.Amount:N2} has been rejected. Reason: {reason}");

                await ShowSuccessAsync("Success", "Payment rejected.");
                await LoadFeesAsync();
            }
            else
            {
                await ShowErrorAsync("Error", "Failed to reject payment.");
            }
        }
    }
    catch (Exception ex)
    {
        await ShowErrorAsync("Error", $"Failed to reject payment: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}
#endregion

#region Invoice Management Methods
private async Task CreateInvoiceAsync()
{
    try
    {
        await NavigateToAsync("//create-invoice");
    }
    catch (Exception ex)
    {
        await ShowErrorAsync("Error", $"Failed to navigate to invoice creation: {ex.Message}");
    }
}

private async Task ApproveInvoiceAsync(Models.Invoice invoice)
{
    if (invoice == null) return;

    try
    {
        var confirm = await ShowConfirmAsync("Approve Invoice",
            $"Approve invoice {invoice.InvoiceNumber} for KSh {invoice.Amount:N2}?");

        if (confirm)
        {
            IsBusy = true;

            invoice.Status = "Approved";
            invoice.ApprovedBy = _currentUserId;
            invoice.ApprovalDate = DateTime.Now;

            var success = await _apiService.UpdateInvoiceAsync(invoice);
            if (success)
            {
                await _notificationService.SendInvoiceApprovalNotificationAsync(
                    invoice.VendorId,
                    invoice.InvoiceNumber,
                    "Approved");

                await ShowSuccessAsync("Success", "Invoice approved successfully.");
                await LoadInvoicesAsync();
            }
            else
            {
                await ShowErrorAsync("Error", "Failed to approve invoice.");
            }
        }
    }
    catch (Exception ex)
    {
        await ShowErrorAsync("Error", $"Failed to approve invoice: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }
}

private async Task RejectInvoiceAsync(Models.Invoice invoice)
{
    if (invoice == null) return;

    try
    {
        var reason = await ShowPromptAsync("Reject Invoice",
            "Please provide a reason for rejection:", "Reject", "Cancel");

        if (!string.IsNullOrEmpty(reason))
        {
            IsBusy = true;

            invoice.Status = "Rejected";
            invoice.Comments = reason;

            var success = await _apiService.UpdateInvoiceAsync(invoice);
            if (success)
            {
                await _notificationService.SendInvoiceApprovalNotificationAsync(
                    invoice.VendorId,
                    invoice.InvoiceNumber,
                    "Rejected");

                await ShowSuccessAsync("Success", "Invoice rejected.");
                await LoadInvoicesAsync();
            }
            else
            {
                await ShowErrorAsync("Error", "Failed to reject invoice.");
            }
        }
    }
    catch (Exception ex)
    {
        await ShowErrorAsync("Error", $"Failed to reject invoice: {ex.Message}");
    }
    finally
    {
        IsBusy = false;
    }