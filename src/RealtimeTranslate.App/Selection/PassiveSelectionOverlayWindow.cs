using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using RealtimeTranslate.Core.Models;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfPen = System.Windows.Media.Pen;
using WpfPoint = System.Windows.Point;

namespace RealtimeTranslate.App.Selection;

public sealed class PassiveSelectionOverlayWindow : Window
{
    private ScreenSelection? _selection;

    public PassiveSelectionOverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = new SolidColorBrush(WpfColor.FromArgb(18, 0, 0, 0));
        Topmost = true;
        ShowInTaskbar = false;
        ShowActivated = false;
        IsHitTestVisible = false;
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;
        Focusable = false;
    }

    public void UpdateSelection(ScreenSelection selection)
    {
        _selection = selection;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        if (_selection is null || _selection.IsEmpty)
        {
            return;
        }

        var rect = ToLocalRect(_selection);
        drawingContext.DrawRectangle(null, new WpfPen(WpfBrushes.White, 2), rect);
        drawingContext.DrawRectangle(new SolidColorBrush(WpfColor.FromArgb(48, 46, 204, 113)), null, rect);
    }

    private Rect ToLocalRect(ScreenSelection selection)
    {
        var transform = PresentationSource.FromVisual(this)?.CompositionTarget?.TransformFromDevice ?? Matrix.Identity;
        var topLeft = transform.Transform(new WpfPoint(selection.X, selection.Y));
        var bottomRight = transform.Transform(new WpfPoint(selection.X + selection.Width, selection.Y + selection.Height));
        return new Rect(
            topLeft.X - Left,
            topLeft.Y - Top,
            Math.Abs(bottomRight.X - topLeft.X),
            Math.Abs(bottomRight.Y - topLeft.Y));
    }
}
