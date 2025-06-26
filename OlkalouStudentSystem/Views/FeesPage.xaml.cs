using OlkalouStudentSystem.ViewModels;

namespace OlkalouStudentSystem.Views;

public partial class FeesPage : ContentPage
{
    private readonly FeesViewModel _viewModel;

    public FeesPage(FeesViewModel viewModel)
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