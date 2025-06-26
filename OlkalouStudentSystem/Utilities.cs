using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;

namespace OlkalouStudentSystem.Utilities
{
    public static class ImageUtilities
    {
        /// <summary>
        /// Creates a standardized image with consistent sizing
        /// </summary>
        public static Image CreateStandardizedImage(string source, double width = 280, double height = 180)
        {
            return new Image
            {
                Source = source,
                Aspect = Aspect.AspectFill,
                WidthRequest = width,
                HeightRequest = height,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.Fill
            };
        }

        /// <summary>
        /// Creates a circular profile image
        /// </summary>
        public static Border CreateCircularImage(string source, double size = 50)
        {
            return new Border
            {
                BackgroundColor = Colors.White,
                WidthRequest = size,
                HeightRequest = size,
                StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
                Content = new Image
                {
                    Source = source,
                    Aspect = Aspect.AspectFill,
                    HorizontalOptions = LayoutOptions.Fill,
                    VerticalOptions = LayoutOptions.Fill
                },
                Shadow = new Shadow
                {
                    Brush = Colors.Black,
                    Offset = new Point(0, 2),
                    Radius = 8,
                    Opacity = 0.3f
                }
            };
        }

        /// <summary>
        /// Creates a card with image and overlay text
        /// </summary>
        public static Border CreateImageCard(string imagePath, string title, string description, double width = 280, double height = 180)
        {
            var card = new Border
            {
                BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#2C2C2C") : Colors.White,
                WidthRequest = width,
                HeightRequest = height,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 20 },
                Shadow = new Shadow
                {
                    Brush = Application.Current?.RequestedTheme == AppTheme.Dark ? Color.FromArgb("#00000030") : Color.FromArgb("#00000015"),
                    Offset = new Point(0, 4),
                    Radius = 15,
                    Opacity = 0.15f
                }
            };

            var grid = new Grid();

            // Main image
            var image = new Image
            {
                Source = imagePath,
                Aspect = Aspect.AspectFill,
                HeightRequest = height * 0.67, // 2/3 of card height
                VerticalOptions = LayoutOptions.Start
            };

            // Overlay with gradient
            var overlay = new Border
            {
                VerticalOptions = LayoutOptions.End,
                HeightRequest = height * 0.33, // 1/3 of card height
                Padding = new Thickness(15, 10),
                Background = new LinearGradientBrush
                {
                    StartPoint = new Point(0, 0),
                    EndPoint = new Point(0, 1),
                    GradientStops = new GradientStopCollection
                    {
                        new GradientStop { Color = Colors.Transparent, Offset = 0.0f },
                        new GradientStop { Color = Color.FromArgb("#80000000"), Offset = 1.0f }
                    }
                }
            };

            var textLayout = new VerticalStackLayout { Spacing = 2 };

            var titleLabel = new Label
            {
                Text = title,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White
            };

            var descriptionLabel = new Label
            {
                Text = description,
                FontSize = 12,
                TextColor = Color.FromArgb("#CCFFFFFF"),
                MaxLines = 2
            };

            textLayout.Children.Add(titleLabel);
            textLayout.Children.Add(descriptionLabel);
            overlay.Content = textLayout;

            grid.Children.Add(image);
            grid.Children.Add(overlay);
            card.Content = grid;

            return card;
        }

        /// <summary>
        /// Gets theme-appropriate colors
        /// </summary>
        public static class ThemeColors
        {
            public static Color CardBackground => Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#2C2C2C")
                : Colors.White;

            public static Color PrimaryText => Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#81C784")
                : Color.FromArgb("#2E7D32");

            public static Color SecondaryText => Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#CCCCCC")
                : Color.FromArgb("#666666");

            public static Color TertiaryText => Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#999999")
                : Color.FromArgb("#999999");

            public static Color ShadowColor => Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#00000030")
                : Color.FromArgb("#00000015");
        }
    }
}