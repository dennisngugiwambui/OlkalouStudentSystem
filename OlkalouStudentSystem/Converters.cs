using System.Globalization;

namespace OlkalouStudentSystem.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ErrorToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var errorMessage = value as string;
            if (!string.IsNullOrEmpty(errorMessage))
            {
                return Color.FromArgb("#F44336"); // Red for error
            }

            // Return theme-appropriate normal color
            return Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#555555")
                : Color.FromArgb("#E0E0E0");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}