using System.Windows;
using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Presentation;

namespace RealtimeTranslate.App.Presentation;

public sealed class WpfOverlayPresenter : IOverlayPresenter
{
    private const double WindowWidth = 380;
    private const double HorizontalMargin = 18;
    private const double VerticalMargin = 22;
    private const double VerticalGap = 12;

    private readonly List<LookupOverlayWindow> _windows = [];

    public void Show(LookupPresentation presentation)
    {
        var window = new LookupOverlayWindow(presentation);
        window.Closed += (_, _) => _windows.Remove(window);
        _windows.Add(window);

        window.Show();
        window.UpdateLayout();

        var placement = FindPlacement(window);
        window.Top = placement.Top;
        window.PlayEntrance(placement.FromLeft, placement.ToLeft);
    }

    private Placement FindPlacement(LookupOverlayWindow window)
    {
        var workArea = SystemParameters.WorkArea;
        var height = Math.Max(window.ActualHeight, 92);
        var rightLeft = workArea.Right - WindowWidth - HorizontalMargin;
        var leftLeft = workArea.Left + HorizontalMargin;

        var rightTop = FindTopForSide(window, workArea.Top + VerticalMargin, workArea.Bottom - VerticalMargin, height, rightLeft);
        if (rightTop is not null)
        {
            return new Placement(workArea.Right + HorizontalMargin, rightLeft, rightTop.Value);
        }

        var leftTop = FindTopForSide(window, workArea.Top + VerticalMargin, workArea.Bottom - VerticalMargin, height, leftLeft);
        if (leftTop is not null)
        {
            return new Placement(workArea.Left - WindowWidth - HorizontalMargin, leftLeft, leftTop.Value);
        }

        return new Placement(workArea.Right + HorizontalMargin, rightLeft, workArea.Top + VerticalMargin);
    }

    private double? FindTopForSide(LookupOverlayWindow current, double minTop, double maxBottom, double height, double sideLeft)
    {
        var top = minTop;
        var windowsOnSide = _windows
            .Where(window => !ReferenceEquals(window, current) && window.IsVisible && Math.Abs(window.Left - sideLeft) < 32)
            .OrderBy(window => window.Top)
            .ToArray();

        foreach (var existing in windowsOnSide)
        {
            if (top + height <= existing.Top - VerticalGap)
            {
                return top;
            }

            top = Math.Max(top, existing.Top + Math.Max(existing.ActualHeight, 92) + VerticalGap);
        }

        return top + height <= maxBottom ? top : null;
    }

    private sealed record Placement(double FromLeft, double ToLeft, double Top);
}
