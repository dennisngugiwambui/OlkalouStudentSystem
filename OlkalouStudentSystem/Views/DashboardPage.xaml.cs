// ===============================
// Views/DashboardPage.xaml.cs - Enhanced Dashboard Page Code Behind
// ===============================
using OlkalouStudentSystem.ViewModels;

namespace OlkalouStudentSystem.Views
{
    public partial class DashboardPage : ContentPage
    {
        private readonly DashboardViewModel _viewModel;

        public DashboardPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        // Parameterless constructor for XAML DataTemplate (if needed)
        public DashboardPage() : this(CreateDefaultViewModel())
        {
        }

        private static DashboardViewModel CreateDefaultViewModel()
        {
            // Create default services - in a real app, these would be injected
            var authService = new OlkalouStudentSystem.Services.AuthService();
            var feesService = new OlkalouStudentSystem.Services.FeesService(authService);
            var apiService = new OlkalouStudentSystem.Services.ApiService();

            return new DashboardViewModel(authService, feesService, apiService);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                await _viewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing dashboard: {ex.Message}");

                // Show error to user
                await DisplayAlert("Error", "Failed to load dashboard. Please try again.", "OK");
            }
        }

        protected override bool OnBackButtonPressed()
        {
            // Handle back button for menu visibility
            if (_viewModel.IsMenuVisible)
            {
                _viewModel.IsMenuVisible = false;
                return true; // Prevent default back navigation
            }

            return base.OnBackButtonPressed();
        }

        // Handle hardware back button on Android
        private async void OnBackButtonPressed(object sender, EventArgs e)
        {
            if (_viewModel.IsMenuVisible)
            {
                _viewModel.IsMenuVisible = false;
            }
            else
            {
                // Confirm exit
                var result = await DisplayAlert("Exit", "Are you sure you want to exit the app?", "Yes", "No");
                if (result)
                {
                    System.Environment.Exit(0);
                }
            }
        }
    }
}