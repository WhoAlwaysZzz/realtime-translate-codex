using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RealtimeTranslate.Core.Models;
using ControlsOrientation = System.Windows.Controls.Orientation;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;

namespace RealtimeTranslate.App.Presentation;

public sealed class LookupOverlayWindow : Window
{
    public LookupOverlayWindow(LookupPresentation presentation)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        SizeToContent = SizeToContent.WidthAndHeight;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;
        Content = BuildContent(presentation);
        MouseLeftButtonUp += (_, _) => Close();
    }

    private static UIElement BuildContent(LookupPresentation presentation)
    {
        var panel = new StackPanel
        {
            Orientation = ControlsOrientation.Vertical,
            MaxWidth = 360,
        };

        panel.Children.Add(new TextBlock
        {
            Text = presentation.Title,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = WpfBrushes.White,
            TextWrapping = TextWrapping.Wrap,
        });

        if (!string.IsNullOrWhiteSpace(presentation.Subtitle))
        {
            panel.Children.Add(new TextBlock
            {
                Text = presentation.Subtitle,
                Margin = new Thickness(0, 2, 0, 8),
                FontSize = 12,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(210, 216, 224)),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        foreach (var definition in presentation.Definitions.Take(4))
        {
            panel.Children.Add(new TextBlock
            {
                Text = definition,
                FontSize = 15,
                Foreground = presentation.IsError
                    ? new SolidColorBrush(WpfColor.FromRgb(255, 204, 204))
                    : new SolidColorBrush(WpfColor.FromRgb(235, 245, 255)),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        return new Border
        {
            Padding = new Thickness(14, 10, 14, 12),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(WpfColor.FromArgb(232, 25, 30, 38)),
            BorderBrush = new SolidColorBrush(WpfColor.FromArgb(160, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Child = panel,
        };
    }
}
