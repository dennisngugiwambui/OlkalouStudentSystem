// ===============================
// App.xaml.cs - Updated with Database Initialization
// ===============================
using OlkalouStudentSystem.Services;
using OlkalouStudentSystem.Models.Data;
using OlkalouStudentSystem.Data;

namespace OlkalouStudentSystem;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        MainPage = new AppShell();
    }

    protected override async void OnStart()
    {
        try
        {
            // Initialize database first
            await InitializeDatabaseAsync();

            // Then handle authentication
            await HandleStartupNavigationAsync();
        }
        catch (Exception ex)
        {
            // Log error and fallback to login
            System.Diagnostics.Debug.WriteLine($"App startup error: {ex.Message}");
            await Shell.Current.GoToAsync("//login");
        }
    }

    /// <summary>
    /// Initialize database tables and migrate data
    /// </summary>
    private async Task InitializeDatabaseAsync()
    {
        try
        {
            var initializer = Handler?.MauiContext?.Services?.GetService<DatabaseInitializer>();
            if (initializer != null)
            {
                await initializer.InitializeAsync();
                System.Diagnostics.Debug.WriteLine("Database initialization completed");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Warning: Database initializer service not found");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            // Don't throw - app should still work even if database init fails
        }
    }

    /// <summary>
    /// Handle startup navigation based on authentication state
    /// </summary>
    private async Task HandleStartupNavigationAsync()
    {
        try
        {
            // Get the AuthService from dependency injection
            var authService = Handler?.MauiContext?.Services?.GetService<AuthService>();
            if (authService != null)
            {
                // Check if user is logged in
                var isLoggedIn = await authService.IsLoggedInAsync();
                if (isLoggedIn)
                {
                    // User is authenticated, go to dashboard
                    await Shell.Current.GoToAsync("//main/dashboard");
                    System.Diagnostics.Debug.WriteLine("User authenticated - navigated to dashboard");
                }
                else
                {
                    // User not authenticated, go to login
                    await Shell.Current.GoToAsync("//login");
                    System.Diagnostics.Debug.WriteLine("User not authenticated - navigated to login");
                }
            }
            else
            {
                // Fallback if service is not available
                System.Diagnostics.Debug.WriteLine("Warning: AuthService not found, navigating to login");
                await Shell.Current.GoToAsync("//login");
            }
        }
        catch (Exception ex)
        {
            // Log error and fallback to login
            System.Diagnostics.Debug.WriteLine($"Startup navigation error: {ex.Message}");
            await Shell.Current.GoToAsync("//login");
        }
    }

    protected override void OnSleep()
    {
        // Handle when your app sleeps
        base.OnSleep();
        System.Diagnostics.Debug.WriteLine("App went to sleep");
    }

    protected override void OnResume()
    {
        // Handle when your app resumes
        base.OnResume();
        System.Diagnostics.Debug.WriteLine("App resumed");

        // Optionally check authentication state when app resumes
        _ = Task.Run(async () =>
        {
            try
            {
                var authService = Handler?.MauiContext?.Services?.GetService<AuthService>();
                if (authService != null)
                {
                    var isLoggedIn = await authService.IsLoggedInAsync();
                    if (!isLoggedIn)
                    {
                        // If user is no longer authenticated, redirect to login
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Shell.Current.GoToAsync("//login");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resume authentication check error: {ex.Message}");
            }
        });
    }
}