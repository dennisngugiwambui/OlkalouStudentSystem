using OlkalouStudentSystem.ViewModels;

namespace OlkalouStudentSystem.Views;

public partial class AssignmentsPage : ContentPage
{
    private readonly AssignmentsViewModel _viewModel;

    public AssignmentsPage(AssignmentsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}