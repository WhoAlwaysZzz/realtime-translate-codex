using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media;
using RealtimeTranslate.Core.Models;
using ControlsOrientation = System.Windows.Controls.Orientation;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace RealtimeTranslate.App.Presentation;

public sealed class LookupOverlayWindow : Window
{
    private static readonly TimeSpan DisplayDuration = TimeSpan.FromSeconds(8);
    private bool _closing;

    public LookupOverlayWindow(LookupPresentation presentation)
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Width = 440;
        SizeToContent = SizeToContent.Height;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;
        UiTheme.StyleWindow(this);
        UiTheme.PrepareEntrance(this);
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
        UiTheme.PlayEntrance(this, TimeSpan.FromMilliseconds(180));

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
        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var accent = new Border
        {
            Width = 3,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            Margin = new Thickness(-14, 0, 0, 0),
            CornerRadius = new CornerRadius(2),
            Background = presentation.IsError ? UiTheme.Brush(UiTheme.Danger) : UiTheme.Brush(UiTheme.Accent),
        };
        Grid.SetRowSpan(accent, 6);
        root.Children.Add(accent);

        var topRow = new Grid();
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        topRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var title = new WpfTextBlock
        {
            Text = presentation.Title,
            FontSize = 21,
            FontWeight = FontWeights.SemiBold,
            Foreground = UiTheme.Brush(UiTheme.TextPrimary),
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 140,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(title, 0);
        topRow.Children.Add(title);

        var primary = new WpfTextBlock
        {
            Text = presentation.PrimaryDefinition,
            Margin = new Thickness(14, 0, 0, 0),
            FontSize = presentation.IsError ? 18 : 25,
            FontWeight = FontWeights.Bold,
            Foreground = presentation.IsError
                ? UiTheme.Brush(UiTheme.Danger)
                : UiTheme.Brush(UiTheme.AccentWarm),
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(primary, 1);
        topRow.Children.Add(primary);

        Grid.SetRow(topRow, 0);
        root.Children.Add(topRow);

        var meta = new WpfTextBlock
        {
            Text = presentation.QueryCount > 0 ? $"第 {presentation.QueryCount} 次 · {presentation.Subtitle}" : presentation.Subtitle,
            Margin = new Thickness(0, 9, 0, 0),
            FontSize = 12,
            Foreground = UiTheme.Brush(UiTheme.TextSecondary),
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(meta, 1);
        root.Children.Add(meta);

        if (presentation.Definitions.Count > 1)
        {
            var extra = new WpfTextBlock
            {
                Text = string.Join(" / ", presentation.Definitions.Skip(1).Take(2)),
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 12,
                Foreground = UiTheme.Brush(UiTheme.TextSecondary),
                TextWrapping = TextWrapping.Wrap,
            };
            Grid.SetRow(extra, 2);
            root.Children.Add(extra);
        }

        if (!string.IsNullOrWhiteSpace(presentation.WordBreakdown))
        {
            var breakdown = CreateInsightText($"构词  {presentation.WordBreakdown}", UiTheme.TextPrimary, 3);
            root.Children.Add(breakdown);
        }

        if (presentation.WordFamily is { Count: > 0 })
        {
            var family = CreateInsightText($"同族  {string.Join(" · ", presentation.WordFamily.Take(3))}", UiTheme.TextSecondary, 4);
            root.Children.Add(family);
        }

        if (!string.IsNullOrWhiteSpace(presentation.ContextHint))
        {
            var context = CreateInsightText($"〔常用于：{presentation.ContextHint}〕", UiTheme.AccentWarm, 5);
            root.Children.Add(context);
        }

        return UiTheme.CreatePanel(root, new Thickness(14, 11, 14, 13));
    }

    private static WpfTextBlock CreateInsightText(string text, System.Windows.Media.Color color, int row)
    {
        var block = new WpfTextBlock
        {
            Text = text,
            Margin = new Thickness(0, 7, 0, 0),
            FontSize = 12,
            Foreground = UiTheme.Brush(color),
            TextWrapping = TextWrapping.Wrap,
        };
        Grid.SetRow(block, row);
        return block;
    }
}
