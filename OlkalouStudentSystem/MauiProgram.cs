// ===============================
// MauiProgram.cs - Updated with All Services
// ===============================
using Microsoft.Extensions.Logging;
using OlkalouStudentSystem;
using OlkalouStudentSystem.Services;
using OlkalouStudentSystem.ViewModels;
using OlkalouStudentSystem.Views;
using OlkalouStudentSystem.Models.Data;
using OlkalouStudentSystem.Data;

namespace OlkalouStudentSystem;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register Core Services (Singletons - one instance for the app lifecycle)
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<FileService>();
        builder.Services.AddSingleton<DatabaseInitializer>();

        // Register Data Services (Singletons)
        builder.Services.AddSingleton<UserRegistrationService>();
        builder.Services.AddSingleton<FeesService>();
        builder.Services.AddSingleton<MarksService>();

        // Register ViewModels (Transient - new instance each time)
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<FeesViewModel>();
        builder.Services.AddTransient<AssignmentsViewModel>();
        builder.Services.AddTransient<LibraryViewModel>();
        builder.Services.AddTransient<ActivitiesViewModel>();

        // Register Views (Transient)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<FeesPage>();
        builder.Services.AddTransient<AssignmentsPage>();
        builder.Services.AddTransient<LibraryPage>();
        builder.Services.AddTransient<ActivitiesPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}