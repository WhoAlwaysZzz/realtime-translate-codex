using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RealtimeTranslate.App.Presentation;
using WpfBrushes = System.Windows.Media.Brushes;
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
        UiTheme.PrepareEntrance(this, 0.97);

        _input = new WpfTextBox
        {
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Padding = new Thickness(12, 8, 12, 8),
            BorderThickness = new Thickness(0),
            Foreground = UiTheme.Brush(UiTheme.TextPrimary),
            Background = UiTheme.Brush(UiTheme.SurfaceElevated),
            CaretBrush = UiTheme.Brush(UiTheme.Accent),
            SelectionBrush = UiTheme.Brush(UiTheme.Accent),
        };
        _input.PreviewKeyDown += OnInputPreviewKeyDown;

        Content = UiTheme.CreatePanel(new Border
        {
            CornerRadius = new CornerRadius(7),
            Background = UiTheme.Brush(UiTheme.SurfaceElevated),
            Child = _input,
        }, new Thickness(1), 9);
    }

    public event EventHandler<string>? Submitted;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        Activate();
        _input.Focus();
        UiTheme.PlayEntrance(this);
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
