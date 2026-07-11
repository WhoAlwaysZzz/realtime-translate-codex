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
        Height = 104;
        Background = WpfBrushes.Transparent;
        AllowsTransparency = true;
        UiTheme.StyleWindow(this);
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        UiTheme.PrepareEntrance(this);

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
            ToolTip = "Type a word. Enter to look up, Tab to complete, Esc to close.",
        };
        _input.PreviewKeyDown += OnInputPreviewKeyDown;

        var layout = new Grid();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.Children.Add(UiTheme.CreateLabel("LOOK UP A WORD"));

        var inputBorder = new Border
        {
            Margin = new Thickness(0, 7, 0, 0),
            CornerRadius = new CornerRadius(6),
            Background = UiTheme.Brush(UiTheme.SurfaceElevated),
            Child = _input,
        };
        Grid.SetRow(inputBorder, 1);
        layout.Children.Add(inputBorder);

        Content = UiTheme.CreatePanel(layout, new Thickness(12));
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
