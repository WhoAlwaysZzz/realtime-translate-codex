namespace RealtimeTranslate.Core.Presentation;

public sealed class OverlayPlacementPlanner
{
    private const double WindowWidth = 380;
    private const double HorizontalMargin = 18;
    private const double VerticalMargin = 22;
    private const double VerticalGap = 12;

    private readonly List<OverlayPlacementSlot> _slots = [];

    public OverlayPlacement Reserve(OverlayWorkArea workArea, double height)
    {
        var boundedHeight = Math.Max(height, 92);
        var rightLeft = workArea.Right - WindowWidth - HorizontalMargin;
        var leftLeft = workArea.Left + HorizontalMargin;

        var rightTop = FindTopForSide(workArea.Top + VerticalMargin, workArea.Bottom - VerticalMargin, boundedHeight, rightLeft);
        if (rightTop is not null)
        {
            return ReserveSlot(workArea.Right + HorizontalMargin, rightLeft, rightTop.Value, boundedHeight);
        }

        var leftTop = FindTopForSide(workArea.Top + VerticalMargin, workArea.Bottom - VerticalMargin, boundedHeight, leftLeft);
        if (leftTop is not null)
        {
            return ReserveSlot(workArea.Left - WindowWidth - HorizontalMargin, leftLeft, leftTop.Value, boundedHeight);
        }

        return ReserveSlot(workArea.Right + HorizontalMargin, rightLeft, workArea.Top + VerticalMargin, boundedHeight);
    }

    public void Release(OverlayPlacementSlot slot)
    {
        _slots.Remove(slot);
    }

    private OverlayPlacement ReserveSlot(double fromLeft, double toLeft, double top, double height)
    {
        var slot = new OverlayPlacementSlot(toLeft, top, height);
        _slots.Add(slot);
        return new OverlayPlacement(fromLeft, toLeft, top, slot);
    }

    private double? FindTopForSide(double minTop, double maxBottom, double height, double sideLeft)
    {
        var top = minTop;
        var slotsOnSide = _slots
            .Where(slot => Math.Abs(slot.Left - sideLeft) < 32)
            .OrderBy(slot => slot.Top)
            .ToArray();

        foreach (var existing in slotsOnSide)
        {
            if (top + height <= existing.Top - VerticalGap)
            {
                return top;
            }

            top = Math.Max(top, existing.Top + existing.Height + VerticalGap);
        }

        return top + height <= maxBottom ? top : null;
    }
}
