using OlkalouStudentSystem.ViewModels;

namespace OlkalouStudentSystem.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    private void OnPhoneEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        var entry = sender as Entry;
        if (entry != null && _viewModel != null)
        {
            var text = e.NewTextValue ?? "";

            // Remove any non-digit characters
            var digitsOnly = new string(text.Where(char.IsDigit).ToArray());

            // Limit to 9 digits
            if (digitsOnly.Length > 9)
            {
                digitsOnly = digitsOnly.Substring(0, 9);
            }

            // Update the display text
            if (entry.Text != digitsOnly)
            {
                entry.Text = digitsOnly;
                return;
            }

            // Update the view model with full phone number
            _viewModel.PhoneNumberDisplay = digitsOnly;
            _viewModel.PhoneNumber = "+254" + digitsOnly;

            // Clear phone error when user starts typing
            if (!string.IsNullOrEmpty(digitsOnly))
            {
                _viewModel.PhoneErrorMessage = "";
            }
        }
    }

    private void OnPhoneEntryFocused(object sender, FocusEventArgs e)
    {
        // Enhanced focus animation
        var focusColor = Application.Current.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#81C784")
            : Color.FromArgb("#2E7D32");

        PhoneBorder.Stroke = focusColor;
        PhoneBorder.StrokeThickness = 2;

        // Add subtle scale animation
        _ = PhoneBorder.ScaleTo(1.02, 200, Easing.CubicOut);
    }

    private void OnPhoneEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel?.PhoneErrorMessage))
        {
            var normalColor = Application.Current.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#555555")
                : Color.FromArgb("#E0E0E0");

            PhoneBorder.Stroke = normalColor;
            PhoneBorder.StrokeThickness = 1;
        }

        // Reset scale
        _ = PhoneBorder.ScaleTo(1.0, 200, Easing.CubicOut);
    }

    private void OnPasswordEntryFocused(object sender, FocusEventArgs e)
    {
        // Enhanced focus animation
        var focusColor = Application.Current.RequestedTheme == AppTheme.Dark
            ? Color.FromArgb("#81C784")
            : Color.FromArgb("#2E7D32");

        PasswordBorder.Stroke = focusColor;
        PasswordBorder.StrokeThickness = 2;

        // Add subtle scale animation
        _ = PasswordBorder.ScaleTo(1.02, 200, Easing.CubicOut);
    }

    private void OnPasswordEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (string.IsNullOrEmpty(_viewModel?.PasswordErrorMessage))
        {
            var normalColor = Application.Current.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#555555")
                : Color.FromArgb("#E0E0E0");

            PasswordBorder.Stroke = normalColor;
            PasswordBorder.StrokeThickness = 1;
        }

        // Reset scale
        _ = PasswordBorder.ScaleTo(1.0, 200, Easing.CubicOut);
    }

    private async void OnSignUpTapped(object sender, TappedEventArgs e)
    {
        // Add a subtle tap animation
        var label = sender as Label;
        if (label != null)
        {
            await label.ScaleTo(0.95, 100);
            await label.ScaleTo(1.0, 100);
        }

        // Navigate to sign up page (implement this route as needed)
        // await Shell.Current.GoToAsync("//signup");

        // For now, show a simple alert
        await DisplayAlert("Sign Up", "Sign up functionality coming soon!", "OK");
    }
}