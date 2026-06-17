using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using RealtimeTranslate.Core.Models;
using ControlsOrientation = System.Windows.Controls.Orientation;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;

namespace RealtimeTranslate.App.Presentation;

public sealed class LookupOverlayWindow : Window
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(5);
    private bool _closing;

    public LookupOverlayWindow(LookupPresentation presentation)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Width = 380;
        SizeToContent = SizeToContent.Height;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;
        Opacity = 0;
        Content = BuildContent(presentation);
        MouseLeftButtonUp += (_, _) => BeginClose();
    }

    public void PlayEntrance(double fromLeft, double toLeft)
    {
        Left = fromLeft;
        BeginAnimation(LeftProperty, new DoubleAnimation
        {
            From = fromLeft,
            To = toLeft,
            Duration = TimeSpan.FromMilliseconds(460),
            EasingFunction = new BackEase
            {
                Amplitude = 0.45,
                EasingMode = EasingMode.EaseOut,
            },
        });
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = DisplayDuration,
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            BeginClose();
        };
        timer.Start();
    }

    public void BeginClose()
    {
        if (_closing)
        {
            return;
        }

        _closing = true;
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(360))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };
        fade.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fade);
    }

    private static UIElement BuildContent(LookupPresentation presentation)
    {
        var root = new StackPanel
        {
            Orientation = ControlsOrientation.Vertical,
        };

        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var title = new TextBlock
        {
            Text = presentation.Title,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Foreground = WpfBrushes.White,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 140,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 0);
        topRow.Children.Add(title);

        var primary = new TextBlock
        {
            Text = presentation.PrimaryDefinition,
            Margin = new Thickness(14, 0, 0, 0),
            FontSize = presentation.IsError ? 18 : 24,
            FontWeight = FontWeights.Bold,
            Foreground = presentation.IsError
                ? new SolidColorBrush(WpfColor.FromRgb(255, 204, 204))
                : new SolidColorBrush(WpfColor.FromRgb(255, 244, 198)),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(primary, 1);
        topRow.Children.Add(primary);

        root.Children.Add(topRow);

        var meta = new TextBlock
        {
            Text = presentation.QueryCount > 0 ? $"第 {presentation.QueryCount} 次 · {presentation.Subtitle}" : presentation.Subtitle,
            Margin = new Thickness(0, 8, 0, 0),
            FontSize = 12,
            Foreground = new SolidColorBrush(WpfColor.FromRgb(183, 190, 202)),
            TextWrapping = TextWrapping.Wrap,
        };
        root.Children.Add(meta);

        if (presentation.Definitions.Count > 1)
        {
            root.Children.Add(new TextBlock
            {
                Text = string.Join(" / ", presentation.Definitions.Skip(1).Take(2)),
                Margin = new Thickness(0, 5, 0, 0),
                FontSize = 12,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(211, 218, 229)),
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
            Child = root,
        };
    }
}
