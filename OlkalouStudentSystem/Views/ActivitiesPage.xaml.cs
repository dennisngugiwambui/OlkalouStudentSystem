using OlkalouStudentSystem.ViewModels;

namespace OlkalouStudentSystem.Views;

public partial class ActivitiesPage : ContentPage
{
    private readonly ActivitiesViewModel _viewModel;

    public ActivitiesPage(ActivitiesViewModel viewModel)
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