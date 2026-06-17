using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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

    public SelectionOverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = new SolidColorBrush(WpfColor.FromArgb(42, 0, 0, 0));
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
        _completion.TrySetResult(selection.IsEmpty ? null : selection);
        Close();
    }

    protected override void OnKeyDown(WpfKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.Escape)
        {
            _completion.TrySetResult(null);
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _completion.TrySetResult(null);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (_start is null || _current is null)
        {
            return;
        }

        var rect = ToRect(_start.Value, _current.Value);
        drawingContext.DrawRectangle(null, new WpfPen(WpfBrushes.White, 2), rect);
        drawingContext.DrawRectangle(new SolidColorBrush(WpfColor.FromArgb(48, 46, 204, 113)), null, rect);
    }

    private ScreenSelection ToScreenSelection(WpfPoint start, WpfPoint end)
    {
        var rect = ToRect(start, end);
        return new ScreenSelection(
            X: (int)Math.Round(Left + rect.X),
            Y: (int)Math.Round(Top + rect.Y),
            Width: (int)Math.Round(rect.Width),
            Height: (int)Math.Round(rect.Height));
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
