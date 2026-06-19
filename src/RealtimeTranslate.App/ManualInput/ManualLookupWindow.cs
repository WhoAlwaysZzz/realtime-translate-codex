using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace RealtimeTranslate.App.ManualInput;

public sealed class ManualLookupWindow : Window
{
    private readonly Func<string, string?> _completePrefix;
    private readonly WpfTextBox _input;

    public ManualLookupWindow(Func<string, string?>? completePrefix = null)
    {
        _completePrefix = completePrefix ?? (_ => null);

        Title = "Manual lookup";
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        Width = 420;
        Height = 74;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        _input = new WpfTextBox
        {
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(12, 8, 12, 8),
            BorderThickness = new Thickness(0),
            Foreground = WpfBrushes.White,
            Background = new SolidColorBrush(WpfColor.FromRgb(34, 39, 48)),
            CaretBrush = WpfBrushes.White,
        };
        _input.PreviewKeyDown += OnInputPreviewKeyDown;

        Content = new Border
        {
            Padding = new Thickness(1),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(WpfColor.FromArgb(180, 255, 255, 255)),
            Child = new Border
            {
                CornerRadius = new CornerRadius(7),
                Background = new SolidColorBrush(WpfColor.FromRgb(34, 39, 48)),
                Child = _input,
            },
        };
    }

    public event EventHandler<string>? Submitted;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Activate();
        _input.Focus();
    }

    private void OnInputPreviewKeyDown(object sender, WpfKeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Tab)
        {
            TryCompleteInput();
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Enter && e.Key != Key.Return)
        {
            return;
        }

        var text = _input.Text.Trim();
        if (text.Length > 0)
        {
            Submitted?.Invoke(this, text);
        }

        Close();
        e.Handled = true;
    }

    private void TryCompleteInput()
    {
        var completion = _completePrefix(_input.Text);
        if (string.IsNullOrWhiteSpace(completion))
        {
            return;
        }

        _input.Text = completion;
        _input.CaretIndex = _input.Text.Length;
    }
}
