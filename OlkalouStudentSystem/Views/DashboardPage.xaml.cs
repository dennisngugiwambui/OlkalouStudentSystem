// ===============================
// Views/DashboardPage.xaml.cs - Enhanced Dashboard Page Code Behind (Fixed)
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem.ViewModels;
using OlkalouStudentSystem.Services;

namespace OlkalouStudentSystem.Views
{
    /// <summary>
    /// Enhanced Dashboard Page with comprehensive error handling and user type support
    /// </summary>
    public partial class DashboardPage : ContentPage
    {
        #region Fields
        private readonly DashboardViewModel _viewModel;
        private readonly ILogger<DashboardPage>? _logger;
        private bool _isInitialized = false;
        #endregion

        #region Constructors
        /// <summary>
        /// Primary constructor with dependency injection
        /// </summary>
        public DashboardPage(DashboardViewModel viewModel, ILogger<DashboardPage>? logger = null)
        {
            InitializeComponent();
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger;
            BindingContext = _viewModel;

            // Set up event handlers
            SetupEventHandlers();
        }

        /// <summary>
        /// Parameterless constructor for XAML DataTemplate and Shell navigation
        /// Creates services using service locator pattern
        /// </summary>
        public DashboardPage() : this(CreateDefaultViewModel())
        {
        }
        #endregion

        #region Initialization
        /// <summary>
        /// Creates a default ViewModel with required services
        /// In production, these should be injected via DI container
        /// </summary>
        private static DashboardViewModel CreateDefaultViewModel()
        {
            try
            {
                // Create default services - in a real app, these would be injected
                var authService = new AuthService();
                var feesService = new FeesService(authService);
                var apiService = new ApiService(authService);

                return new DashboardViewModel(authService, feesService, apiService);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating default ViewModel: {ex.Message}");

                // Fallback to minimal services
                var authService = new AuthService();
                var feesService = new FeesService(authService);
                var apiService = new ApiService(authService);

                return new DashboardViewModel(authService, feesService, apiService);
            }
        }

        /// <summary>
        /// Set up event handlers for the page
        /// </summary>
        private void SetupEventHandlers()
        {
            try
            {
                // Handle ViewModel property changes if needed
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error setting up event handlers");
            }
        }
        #endregion

        #region Lifecycle Methods
        /// <summary>
        /// Called when the page appears
        /// </summary>
        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                _logger?.LogInformation("Dashboard page appearing");

                // Initialize only once
                if (!_isInitialized)
                {
                    await InitializeDashboardAsync();
                    _isInitialized = true;
                }
                else
                {
                    // Refresh data on subsequent appearances
                    await RefreshDashboardAsync();
                }

                // Update the welcome message based on time
                UpdateWelcomeMessage();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during page appearance");
                await HandleInitializationErrorAsync(ex);
            }
        }

        /// <summary>
        /// Called when the page disappears
        /// </summary>
        protected override void OnDisappearing()
        {
            try
            {
                base.OnDisappearing();

                // Close menu if open
                if (_viewModel.IsMenuVisible)
                {
                    _viewModel.IsMenuVisible = false;
                }

                _logger?.LogInformation("Dashboard page disappearing");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during page disappearance");
            }
        }

        /// <summary>
        /// Handle hardware back button press
        /// </summary>
        protected override bool OnBackButtonPressed()
        {
            try
            {
                // Handle back button for menu visibility
                if (_viewModel.IsMenuVisible)
                {
                    _viewModel.IsMenuVisible = false;
                    return true; // Prevent default back navigation
                }

                // Show exit confirmation
                _ = Task.Run(async () => await ShowExitConfirmationAsync());
                return true; // Prevent immediate exit
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling back button press");
                return base.OnBackButtonPressed();
            }
        }
        #endregion

        #region Initialization Methods
        /// <summary>
        /// Initialize the dashboard with proper error handling
        /// </summary>
        private async Task InitializeDashboardAsync()
        {
            try
            {
                _logger?.LogInformation("Initializing dashboard");

                // Show loading state
                if (_viewModel != null)
                {
                    _viewModel.IsBusy = true;
                }

                // Initialize the ViewModel
                await _viewModel.InitializeAsync();

                _logger?.LogInformation("Dashboard initialized successfully");
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("Dashboard initialization was cancelled");
                await ShowMessageAsync("Information", "Dashboard loading was cancelled.");
            }
            catch (UnauthorizedAccessException)
            {
                _logger?.LogError("Unauthorized access during dashboard initialization");
                await HandleUnauthorizedAccessAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error during dashboard initialization");
                await HandleInitializationErrorAsync(ex);
            }
            finally
            {
                if (_viewModel != null)
                {
                    _viewModel.IsBusy = false;
                }
            }
        }

        /// <summary>
        /// Refresh dashboard data
        /// </summary>
        private async Task RefreshDashboardAsync()
        {
            try
            {
                _logger?.LogInformation("Refreshing dashboard data");

                if (_viewModel?.RefreshCommand?.CanExecute(null) == true)
                {
                    _viewModel.RefreshCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error refreshing dashboard");
                await ShowMessageAsync("Refresh Error", "Failed to refresh dashboard data. Please try again.");
            }
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Handle ViewModel property changes
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                switch (e.PropertyName)
                {
                    case nameof(DashboardViewModel.IsMenuVisible):
                        HandleMenuVisibilityChange();
                        break;
                    case nameof(DashboardViewModel.UserType):
                        HandleUserTypeChange();
                        break;
                    case nameof(DashboardViewModel.SelectedTab):
                        HandleTabSelectionChange();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling property change: {PropertyName}", e.PropertyName);
            }
        }

        /// <summary>
        /// Handle menu visibility changes
        /// </summary>
        private void HandleMenuVisibilityChange()
        {
            try
            {
                if (_viewModel.IsMenuVisible)
                {
                    _logger?.LogDebug("Menu opened");
                    // Could add animation or other effects here
                }
                else
                {
                    _logger?.LogDebug("Menu closed");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling menu visibility change");
            }
        }

        /// <summary>
        /// Handle user type changes
        /// </summary>
        [Obsolete]
        private void HandleUserTypeChange()
        {
            try
            {
                _logger?.LogInformation("User type changed to: {UserType}", _viewModel.UserType);

                // Update title or other UI elements based on user type
                Title = $"{_viewModel.UserType} Dashboard";

                // Update navigation bar title
                Shell.SetTitleView(this, CreateTitleView());
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling user type change");
            }
        }

        /// <summary>
        /// Handle tab selection changes
        /// </summary>
        private void HandleTabSelectionChange()
        {
            try
            {
                if (_viewModel.SelectedTab != null)
                {
                    _logger?.LogDebug("Tab selected: {TabId}", _viewModel.SelectedTab.Id);

                    // Update UI based on selected tab
                    UpdateTabContent();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling tab selection change");
            }
        }

        /// <summary>
        /// Handle hardware back button press with exit confirmation
        /// </summary>
        private async void OnBackButtonPressed(object sender, EventArgs e)
        {
            try
            {
                if (_viewModel.IsMenuVisible)
                {
                    _viewModel.IsMenuVisible = false;
                }
                else
                {
                    await ShowExitConfirmationAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling back button press event");
            }
        }
        #endregion

        #region UI Helper Methods
        /// <summary>
        /// Create custom title view for the navigation bar
        /// </summary>
        [Obsolete]
        private View CreateTitleView()
        {
            try
            {
                var titleLabel = new Label
                {
                    Text = $"{_viewModel.UserType} Dashboard",
                    FontSize = 18,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Colors.White,
                    HorizontalOptions = LayoutOptions.CenterAndExpand,
                    VerticalOptions = LayoutOptions.CenterAndExpand
                };

                return titleLabel;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error creating title view");
                return new Label { Text = "Dashboard" };
            }
        }

        /// <summary>
        /// Update tab content based on selection
        /// </summary>
        private void UpdateTabContent()
        {
            try
            {
                // This method would update the main content area based on the selected tab
                // Implementation depends on your UI structure
                _logger?.LogDebug("Updating tab content for: {TabId}", _viewModel.SelectedTab?.Id);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating tab content");
            }
        }

        /// <summary>
        /// Update welcome message based on time of day
        /// </summary>
        private void UpdateWelcomeMessage()
        {
            try
            {
                var hour = DateTime.Now.Hour;
                var greeting = hour switch
                {
                    >= 5 and < 12 => "Good Morning",
                    >= 12 and < 17 => "Good Afternoon",
                    >= 17 and < 21 => "Good Evening",
                    _ => "Good Night"
                };

                var message = $"{greeting}, {_viewModel.UserName}";

                if (_viewModel != null)
                {
                    _viewModel.WelcomeMessage = message;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating welcome message");
            }
        }

        /// <summary>
        /// Show exit confirmation dialog
        /// </summary>
        private async Task ShowExitConfirmationAsync()
        {
            try
            {
                var result = await DisplayAlert(
                    "Exit App",
                    "Are you sure you want to exit the application?",
                    "Exit",
                    "Cancel");

                if (result)
                {
                    _logger?.LogInformation("User confirmed app exit");

                    // Close the application
                    Application.Current?.Quit();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing exit confirmation");
            }
        }

        /// <summary>
        /// Show a simple message dialog
        /// </summary>
        private async Task ShowMessageAsync(string title, string message)
        {
            try
            {
                await DisplayAlert(title, message, "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing message dialog");
                System.Diagnostics.Debug.WriteLine($"Failed to show message: {title} - {message}");
            }
        }

        /// <summary>
        /// Show a confirmation dialog
        /// </summary>
        private async Task<bool> ShowConfirmationAsync(string title, string message)
        {
            try
            {
                return await DisplayAlert(title, message, "Yes", "No");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error showing confirmation dialog");
                return false;
            }
        }
        #endregion

        #region Error Handling
        /// <summary>
        /// Handle initialization errors with user-friendly messages
        /// </summary>
        private async Task HandleInitializationErrorAsync(Exception ex)
        {
            try
            {
                string userMessage;
                string title;

                switch (ex)
                {
                    case TimeoutException:
                        title = "Connection Timeout";
                        userMessage = "The connection timed out. Please check your internet connection and try again.";
                        break;
                    case UnauthorizedAccessException:
                        title = "Access Denied";
                        userMessage = "You don't have permission to access this content. Please login again.";
                        break;
                    case System.Net.NetworkInformation.NetworkInformationException:
                        title = "Network Error";
                        userMessage = "Unable to connect to the server. Please check your internet connection.";
                        break;
                    default:
                        title = "Loading Error";
                        userMessage = "Failed to load dashboard. Please try again or contact support if the problem persists.";
                        break;
                }

                await ShowMessageAsync(title, userMessage);

                // Offer retry option
                var retry = await ShowConfirmationAsync("Retry", "Would you like to try loading the dashboard again?");
                if (retry)
                {
                    await InitializeDashboardAsync();
                }
            }
            catch (Exception handlingEx)
            {
                _logger?.LogError(handlingEx, "Error handling initialization error");

                // Last resort - basic error message
                await DisplayAlert("Error", "An unexpected error occurred. Please restart the app.", "OK");
            }
        }

        /// <summary>
        /// Handle unauthorized access
        /// </summary>
        private async Task HandleUnauthorizedAccessAsync()
        {
            try
            {
                await ShowMessageAsync("Session Expired", "Your session has expired. Please login again.");

                // Navigate to login page
                await Shell.Current.GoToAsync("//login");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling unauthorized access");
            }
        }
        #endregion

        #region Role-specific UI Updates

        /// <summary>
        /// Update UI based on user role
        /// </summary>
        private void UpdateUIForUserRole()
        {
            try
            {
                switch (_viewModel.UserType)
                {
                    case "Student":
                        UpdateStudentUI();
                        break;
                    case "Teacher":
                        UpdateTeacherUI();
                        break;
                    case "Principal":
                        UpdatePrincipalUI();
                        break;
                    case "Secretary":
                        UpdateSecretaryUI();
                        break;
                    case "Bursar":
                        UpdateBursarUI();
                        break;
                    default:
                        UpdateStaffUI();
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating UI for user role: {UserType}", _viewModel.UserType);
            }
        }

        /// <summary>
        /// Update UI for student view
        /// </summary>
        private void UpdateStudentUI()
        {
            try
            {
                // Hide/show elements specific to students
                // This would be implemented based on your XAML structure
                _logger?.LogDebug("Updating UI for student view");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating student UI");
            }
        }

        /// <summary>
        /// Update UI for teacher view
        /// </summary>
        private void UpdateTeacherUI()
        {
            try
            {
                // Hide/show elements specific to teachers
                _logger?.LogDebug("Updating UI for teacher view");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating teacher UI");
            }
        }

        /// <summary>
        /// Update UI for principal view
        /// </summary>
        private void UpdatePrincipalUI()
        {
            try
            {
                // Hide/show elements specific to principal
                _logger?.LogDebug("Updating UI for principal view");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating principal UI");
            }
        }

        /// <summary>
        /// Update UI for secretary view
        /// </summary>
        private void UpdateSecretaryUI()
        {
            try
            {
                // Hide/show elements specific to secretary
                _logger?.LogDebug("Updating UI for secretary view");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating secretary UI");
            }
        }

        /// <summary>
        /// Update UI for bursar view
        /// </summary>
        private void UpdateBursarUI()
        {
            try
            {
                // Hide/show elements specific to bursar
                _logger?.LogDebug("Updating UI for bursar view");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating bursar UI");
            }
        }

        /// <summary>
        /// Update UI for general staff view
        /// </summary>
        private void UpdateStaffUI()
        {
            try
            {
                // Hide/show elements specific to general staff
                _logger?.LogDebug("Updating UI for staff view");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating staff UI");
            }
        }

        #endregion

        #region Navigation Helper Methods

        /// <summary>
        /// Handle navigation to different sections based on user role
        /// </summary>
        /// <param name="section">The section to navigate to</param>
        private async Task NavigateToSectionAsync(string section)
        {
            try
            {
                switch (section.ToLower())
                {
                    case "assignments":
                        await NavigateToAssignmentsAsync();
                        break;
                    case "fees":
                        await NavigateToFeesAsync();
                        break;
                    case "library":
                        await NavigateToLibraryAsync();
                        break;
                    case "activities":
                        await NavigateToActivitiesAsync();
                        break;
                    case "students":
                        await NavigateToStudentsAsync();
                        break;
                    case "teachers":
                        await NavigateToTeachersAsync();
                        break;
                    case "reports":
                        await NavigateToReportsAsync();
                        break;
                    case "statistics":
                        await NavigateToStatisticsAsync();
                        break;
                    default:
                        _logger?.LogWarning("Unknown section: {Section}", section);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to section: {Section}", section);
                await ShowMessageAsync("Navigation Error", $"Failed to navigate to {section}");
            }
        }

        /// <summary>
        /// Navigate to assignments page
        /// </summary>
        private async Task NavigateToAssignmentsAsync()
        {
            await Shell.Current.GoToAsync("//assignments");
        }

        /// <summary>
        /// Navigate to fees page
        /// </summary>
        private async Task NavigateToFeesAsync()
        {
            await Shell.Current.GoToAsync("//fees");
        }

        /// <summary>
        /// Navigate to library page
        /// </summary>
        private async Task NavigateToLibraryAsync()
        {
            await Shell.Current.GoToAsync("//library");
        }

        /// <summary>
        /// Navigate to activities page
        /// </summary>
        private async Task NavigateToActivitiesAsync()
        {
            await Shell.Current.GoToAsync("//activities");
        }

        /// <summary>
        /// Navigate to students page
        /// </summary>
        private async Task NavigateToStudentsAsync()
        {
            await Shell.Current.GoToAsync("//students");
        }

        /// <summary>
        /// Navigate to teachers page
        /// </summary>
        private async Task NavigateToTeachersAsync()
        {
            await Shell.Current.GoToAsync("//teachers");
        }

        /// <summary>
        /// Navigate to reports page
        /// </summary>
        private async Task NavigateToReportsAsync()
        {
            await Shell.Current.GoToAsync("//reports");
        }

        /// <summary>
        /// Navigate to statistics page
        /// </summary>
        private async Task NavigateToStatisticsAsync()
        {
            await Shell.Current.GoToAsync("//statistics");
        }

        #endregion

        #region Performance Optimization

        /// <summary>
        /// Optimize UI performance for different user types
        /// </summary>
        private void OptimizeUIPerformance()
        {
            try
            {
                // Enable/disable UI elements based on user type to improve performance
                switch (_viewModel.UserType)
                {
                    case "Student":
                        // Only enable student-specific elements
                        break;
                    case "Teacher":
                        // Only enable teacher-specific elements
                        break;
                    case "Principal":
                        // Enable all elements for principal
                        break;
                    default:
                        // Enable based on role permissions
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error optimizing UI performance");
            }
        }

        /// <summary>
        /// Load data lazily based on user interactions
        /// </summary>
        private async Task LoadDataLazilyAsync(string dataType)
        {
            try
            {
                // Load data only when needed to improve performance
                switch (dataType)
                {
                    case "assignments":
                        if (_viewModel.PendingAssignments.Count == 0)
                        {
                            // Load assignments data
                        }
                        break;
                    case "fees":
                        if (_viewModel.CurrentFees == null)
                        {
                            // Load fees data
                        }
                        break;
                        // Add more cases as needed
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading data lazily: {DataType}", dataType);
            }
        }

        #endregion

        #region Memory Management
        /// <summary>
        /// Clean up resources when page is disposed
        /// </summary>
        protected override void OnHandlerDisconnected()
        {
            try
            {
                base.OnHandlerDisconnected();

                // Unsubscribe from events
                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                }

                _logger?.LogInformation("Dashboard page handler disconnected");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during handler disconnection");
            }
        }

        /// <summary>
        /// Dispose of resources
        /// </summary>
        protected override void OnDisappearing()
        {
            try
            {
                base.OnDisappearing();

                // Clear any cached data to free memory
                ClearCachedData();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during page disappearing");
            }
        }

        /// <summary>
        /// Clear cached data to free memory
        /// </summary>
        private void ClearCachedData()
        {
            try
            {
                // Clear collections that are no longer needed
                // This helps with memory management
                _logger?.LogDebug("Clearing cached data");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error clearing cached data");
            }
        }
        #endregion

        #region Accessibility
        /// <summary>
        /// Configure accessibility features
        /// </summary>
        private void ConfigureAccessibility()
        {
            try
            {
                // Set semantic descriptions for screen readers
                SemanticProperties.SetDescription(this, $"Dashboard page for {_viewModel.UserType} showing overview and quick actions");

                // Configure accessibility for role-specific content
                ConfigureRoleSpecificAccessibility();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error configuring accessibility");
            }
        }

        /// <summary>
        /// Configure accessibility features specific to user roles
        /// </summary>
        private void ConfigureRoleSpecificAccessibility()
        {
            try
            {
                switch (_viewModel.UserType)
                {
                    case "Student":
                        // Configure student-specific accessibility
                        break;
                    case "Teacher":
                        // Configure teacher-specific accessibility
                        break;
                    case "Principal":
                        // Configure principal-specific accessibility
                        break;
                        // Add more cases as needed
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error configuring role-specific accessibility");
            }
        }
        #endregion

        #region Event Handlers for UI Elements

        /// <summary>
        /// Handle tab selection from UI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTabSelected(object sender, EventArgs e)
        {
            try
            {
                if (sender is View view && view.BindingContext is DashboardTab tab)
                {
                    _viewModel.SelectedTab = tab;

                    // Update all tabs to show only the selected one as active
                    foreach (var dashboardTab in _viewModel.DashboardTabs)
                    {
                        dashboardTab.IsSelected = (dashboardTab.Id == tab.Id);
                    }

                    // Load data for selected tab if needed
                    _ = Task.Run(async () => await LoadDataLazilyAsync(tab.Id));
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling tab selection");
            }
        }

        /// <summary>
        /// Handle refresh gesture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnRefreshRequested(object sender, EventArgs e)
        {
            try
            {
                await RefreshDashboardAsync();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error handling refresh request");
                await ShowMessageAsync("Refresh Error", "Failed to refresh dashboard data");
            }
        }

        /// <summary>
        /// Handle menu toggle
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMenuToggle(object sender, EventArgs e)
        {
            try
            {
                _viewModel.IsMenuVisible = !_viewModel.IsMenuVisible;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error toggling menu");
            }
        }

        #endregion
    }
}