using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using RealtimeTranslate.App.Presentation;
using RealtimeTranslate.Core.Models;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfCursors = System.Windows.Input.Cursors;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPen = System.Windows.Media.Pen;
using WpfPoint = System.Windows.Point;

namespace RealtimeTranslate.App.Selection;

public sealed class SelectionOverlayWindow : Window
{
    private readonly TaskCompletionSource<ScreenSelection?> _completion = new();
    private WpfPoint? _start;
    private WpfPoint? _current;
    private ScreenSelection? _result;

    public SelectionOverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = new SolidColorBrush(WpfColor.FromArgb(30, 0, 0, 0));
        Topmost = true;
        ShowInTaskbar = false;
        Cursor = WpfCursors.Cross;
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        Focusable = true;
    }

    public Task<ScreenSelection?> Completion => _completion.Task;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        Activate();
        Focus();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        _start = e.GetPosition(this);
        _current = _start;
        CaptureMouse();
        InvalidateVisual();
    }

    protected override void OnMouseMove(WpfMouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_start is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        _current = e.GetPosition(this);
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_start is null)
        {
            return;
        }

        _current = e.GetPosition(this);
        ReleaseMouseCapture();
        var selection = ToScreenSelection(_start.Value, _current.Value);
        _result = selection.IsEmpty ? null : selection;
        Close();
    }

    protected override void OnKeyDown(WpfKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            _result = null;
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _completion.TrySetResult(_result);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (_start is null || _current is null)
        {
            return;
        }

        var rect = ToRect(_start.Value, _current.Value);
        drawingContext.DrawRectangle(UiTheme.Brush(WpfColor.FromArgb(34, UiTheme.Accent.R, UiTheme.Accent.G, UiTheme.Accent.B)), null, rect);
        drawingContext.DrawRectangle(null, new WpfPen(WpfBrushes.Black, 4), rect);
        drawingContext.DrawRectangle(null, new WpfPen(UiTheme.Brush(UiTheme.Accent), 2), rect);
    }

    private ScreenSelection ToScreenSelection(WpfPoint start, WpfPoint end)
    {
        var rect = ToRect(start, end);
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new WpfPoint(Left + rect.Left, Top + rect.Top));
        var bottomRight = transform.Transform(new WpfPoint(Left + rect.Right, Top + rect.Bottom));

        return new ScreenSelection(
            X: (int)Math.Round(Math.Min(topLeft.X, bottomRight.X)),
            Y: (int)Math.Round(Math.Min(topLeft.Y, bottomRight.Y)),
            Width: (int)Math.Round(Math.Abs(bottomRight.X - topLeft.X)),
            Height: (int)Math.Round(Math.Abs(bottomRight.Y - topLeft.Y)));
    }

    private static Rect ToRect(WpfPoint start, WpfPoint end)
    {
        var x = Math.Min(start.X, end.X);
        var y = Math.Min(start.Y, end.Y);
        var width = Math.Abs(start.X - end.X);
        var height = Math.Abs(start.Y - end.Y);
        return new Rect(x, y, width, height);
    }
}
