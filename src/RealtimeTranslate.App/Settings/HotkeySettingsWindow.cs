using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RealtimeTranslate.App.Presentation;
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
        Width = 400;
        Height = 214;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ResizeMode = ResizeMode.NoResize;
        Topmost = true;
        WindowStyle = WindowStyle.None;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;
        UiTheme.StyleWindow(this);
        UiTheme.PrepareEntrance(this);

        _hotkeyText = new TextBlock
        {
            Text = currentHotkey.ToString(),
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = UiTheme.Brush(UiTheme.AccentWarm),
            HorizontalAlignment = WpfHorizontalAlignment.Center,
            Margin = new Thickness(0, 16, 0, 12),
        };

        var saveButton = UiTheme.CreateButton("Save", () =>
        {
            DialogResult = true;
            Close();
        });
        saveButton.IsDefault = true;
        saveButton.Margin = new Thickness(8, 0, 0, 0);
        var cancelButton = UiTheme.CreateButton("Cancel", Close);
        cancelButton.IsCancel = true;

        var buttons = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = WpfHorizontalAlignment.Right,
            Children = { saveButton, cancelButton },
        };

        var content = new StackPanel
        {
            Children =
            {
                UiTheme.CreateLabel("KEYBOARD SHORTCUT"),
                new TextBlock
                {
                    Text = "Press the shortcut now. F-keys work alone; other keys need Ctrl, Alt, Shift, or Win.",
                    Margin = new Thickness(0, 8, 0, 0),
                    Foreground = UiTheme.Brush(UiTheme.TextSecondary),
                    TextWrapping = TextWrapping.Wrap,
                },
                _hotkeyText,
                buttons,
            },
        };
        Content = UiTheme.CreatePanel(content, new Thickness(18));

        PreviewKeyDown += OnPreviewKeyDown;
    }

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        UiTheme.PlayEntrance(this);
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
