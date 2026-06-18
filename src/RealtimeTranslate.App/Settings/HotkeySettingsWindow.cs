using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace RealtimeTranslate.App.Settings;

public sealed class HotkeySettingsWindow : Window
{
    private readonly TextBlock _hotkeyText;
    private HotkeyGesture _capturedHotkey;

    public HotkeySettingsWindow(HotkeyGesture currentHotkey)
    {
        _capturedHotkey = currentHotkey;
        Title = "Configure hotkey";
        Width = 360;
        Height = 190;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;

        _hotkeyText = new TextBlock
        {
            Text = currentHotkey.ToString(),
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = WpfBrushes.White,
            HorizontalAlignment = WpfHorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 12),
        };

        var saveButton = new WpfButton
        {
            Content = "Save",
            Width = 88,
            Height = 32,
            IsDefault = true,
            Margin = new Thickness(8),
        };
        saveButton.Click += (_, _) =>
        {
            DialogResult = true;
            Close();
        };

        var cancelButton = new WpfButton
        {
            Content = "Cancel",
            Width = 88,
            Height = 32,
            IsCancel = true,
            Margin = new Thickness(8),
        };

        var buttons = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = WpfHorizontalAlignment.Center,
            Children = { saveButton, cancelButton },
        };

        Content = new Border
        {
            Background = new SolidColorBrush(WpfColor.FromRgb(32, 37, 46)),
            Padding = new Thickness(18),
            Child = new StackPanel
            {
                Children =
                {
                    new TextBlock
                    {
                        Text = "Press a new shortcut. F-keys can be used alone; other keys should use Ctrl, Alt, Shift, or Win.",
                        Foreground = new SolidColorBrush(WpfColor.FromRgb(210, 216, 224)),
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = WpfHorizontalAlignment.Center,
                    },
                    _hotkeyText,
                    buttons,
                },
            },
        };

        PreviewKeyDown += OnPreviewKeyDown;
    }

    public HotkeyGesture CapturedHotkey => _capturedHotkey;

    private void OnPreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            return;
        }

        var modifiers = Keyboard.Modifiers;
        var hotkey = new HotkeyGesture(key, modifiers);
        if (hotkey.IsValid)
        {
            _capturedHotkey = hotkey;
            _hotkeyText.Text = hotkey.ToString();
        }

        e.Handled = true;
    }
}
