using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using WpfBorder = System.Windows.Controls.Border;
using WpfButton = System.Windows.Controls.Button;
using WpfButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using WpfColor = System.Windows.Media.Color;
using WpfContentPresenter = System.Windows.Controls.ContentPresenter;
using WpfControl = System.Windows.Controls.Control;
using WpfCursors = System.Windows.Input.Cursors;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfFontFamily = System.Windows.Media.FontFamily;
using WpfPoint = System.Windows.Point;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfVerticalAlignment = System.Windows.VerticalAlignment;
using WpfBrushes = System.Windows.Media.Brushes;

namespace RealtimeTranslate.App.Presentation;

public static class UiTheme
{
    public static readonly WpfColor Surface = WpfColor.FromRgb(24, 29, 36);
    public static readonly WpfColor SurfaceElevated = WpfColor.FromRgb(32, 38, 48);
    public static readonly WpfColor SurfacePressed = WpfColor.FromRgb(45, 53, 66);
    public static readonly WpfColor BorderColor = WpfColor.FromArgb(110, 255, 255, 255);
    public static readonly WpfColor TextPrimary = WpfColor.FromRgb(248, 250, 252);
    public static readonly WpfColor TextSecondary = WpfColor.FromRgb(184, 194, 210);
    public static readonly WpfColor TextMuted = WpfColor.FromRgb(139, 151, 168);
    public static readonly WpfColor Accent = WpfColor.FromRgb(139, 211, 214);
    public static readonly WpfColor AccentWarm = WpfColor.FromRgb(255, 231, 176);
    public static readonly WpfColor Danger = WpfColor.FromRgb(255, 190, 190);
    public const string FontFamilyName = "Segoe UI";
    public const double PanelRadius = 8;

    public static SolidColorBrush Brush(WpfColor color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    public static WpfBorder CreatePanel(UIElement child, Thickness padding, double radius = PanelRadius)
    {
        return new WpfBorder
        {
            Padding = padding,
            CornerRadius = new CornerRadius(radius),
            Background = Brush(WpfColor.FromArgb(244, Surface.R, Surface.G, Surface.B)),
            BorderBrush = Brush(BorderColor),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect
            {
                BlurRadius = 18,
                ShadowDepth = 5,
                Opacity = 0.3,
                Color = WpfColor.FromRgb(0, 0, 0),
            },
            Child = child,
        };
    }

    public static WpfButton CreateButton(string text, Action action)
    {
        var button = new WpfButton
        {
            Content = text,
            Padding = new Thickness(13, 6, 13, 7),
            MinWidth = 64,
            BorderThickness = new Thickness(0),
            Foreground = Brush(TextPrimary),
            Background = Brush(SurfaceElevated),
            Cursor = WpfCursors.Hand,
            Style = CreateButtonStyle(),
        };
        button.Click += (_, _) => action();
        return button;
    }

    public static WpfTextBlock CreateLabel(string text)
    {
        return new WpfTextBlock
        {
            Text = text.ToUpperInvariant(),
            FontFamily = new WpfFontFamily(FontFamilyName),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brush(TextMuted),
        };
    }

    public static void StyleWindow(Window window)
    {
        window.FontFamily = new WpfFontFamily(FontFamilyName);
        window.Foreground = Brush(TextPrimary);
        window.Background = WpfBrushes.Transparent;
    }

    public static void PrepareEntrance(Window window, double scale = 0.98)
    {
        window.Opacity = 0;
        window.RenderTransformOrigin = new WpfPoint(0.5, 0.5);
        window.RenderTransform = new ScaleTransform(scale, scale);
    }

    public static void PlayEntrance(Window window, TimeSpan? duration = null)
    {
        var animationDuration = duration ?? TimeSpan.FromMilliseconds(180);
        window.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, animationDuration)
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        });

        if (window.RenderTransform is ScaleTransform scale)
        {
            var scaleAnimation = new DoubleAnimation(1, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new BackEase
                {
                    Amplitude = 0.22,
                    EasingMode = EasingMode.EaseOut,
                },
            };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
        }
    }

    private static Style CreateButtonStyle()
    {
        var border = new FrameworkElementFactory(typeof(WpfBorder));
        border.SetValue(WpfBorder.CornerRadiusProperty, new CornerRadius(6));
        border.SetBinding(WpfBorder.BackgroundProperty, new System.Windows.Data.Binding(nameof(WpfControl.Background))
        {
            RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent,
        });

        var content = new FrameworkElementFactory(typeof(WpfContentPresenter));
        content.SetValue(FrameworkElement.HorizontalAlignmentProperty, WpfHorizontalAlignment.Center);
        content.SetValue(FrameworkElement.VerticalAlignmentProperty, WpfVerticalAlignment.Center);
        border.AppendChild(content);

        var template = new ControlTemplate(typeof(WpfButton))
        {
            VisualTree = border,
        };

        var style = new Style(typeof(WpfButton));
        style.Setters.Add(new Setter(WpfControl.TemplateProperty, template));
        style.Setters.Add(new Setter(WpfControl.FontSizeProperty, 12d));
        style.Setters.Add(new Setter(WpfControl.FontWeightProperty, FontWeights.SemiBold));
        style.Triggers.Add(new Trigger
        {
            Property = UIElement.IsMouseOverProperty,
            Value = true,
            Setters = { new Setter(WpfControl.BackgroundProperty, Brush(SurfacePressed)) },
        });
        style.Triggers.Add(new Trigger
        {
            Property = WpfButtonBase.IsPressedProperty,
            Value = true,
            Setters = { new Setter(WpfControl.BackgroundProperty, Brush(Accent)) },
        });
        return style;
    }
}
