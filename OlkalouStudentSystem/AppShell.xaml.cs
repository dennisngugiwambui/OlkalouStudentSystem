using OlkalouStudentSystem.Views;

namespace OlkalouStudentSystem;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private void RegisterRoutes()
    {
        // Register individual page routes for navigation
        Routing.RegisterRoute("login", typeof(LoginPage));
        Routing.RegisterRoute("dashboard", typeof(DashboardPage));
        Routing.RegisterRoute("fees", typeof(FeesPage));
        Routing.RegisterRoute("assignments", typeof(AssignmentsPage));
        Routing.RegisterRoute("library", typeof(LibraryPage));
        Routing.RegisterRoute("activities", typeof(ActivitiesPage));

        // Register additional routes for deeper navigation
        Routing.RegisterRoute("fees/details", typeof(FeesPage));
        Routing.RegisterRoute("assignments/details", typeof(AssignmentsPage));
        Routing.RegisterRoute("library/details", typeof(LibraryPage));
        Routing.RegisterRoute("activities/details", typeof(ActivitiesPage));
    }
}