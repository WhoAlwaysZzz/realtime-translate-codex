using System.Windows;
using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Presentation;

namespace RealtimeTranslate.App.Presentation;

public sealed class WpfOverlayPresenter : IOverlayPresenter
{
    private readonly OverlayPlacementPlanner _placementPlanner = new();

    public void Show(LookupPresentation presentation)
    {
        var window = new LookupOverlayWindow(presentation);
        window.Show();
        window.UpdateLayout();

        var height = GetWindowHeight(window);
        var placement = _placementPlanner.Reserve(GetWorkArea(), height);
        window.Closed += (_, _) => _placementPlanner.Release(placement.Slot);

        window.Top = placement.Top;
        window.PlayEntrance(placement.FromLeft, placement.ToLeft);
    }

    private static OverlayWorkArea GetWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        return new OverlayWorkArea(workArea.Left, workArea.Top, workArea.Right, workArea.Bottom);
    }

    private static double GetWindowHeight(LookupOverlayWindow window)
    {
        return Math.Max(window.ActualHeight, 92);
    }
}
