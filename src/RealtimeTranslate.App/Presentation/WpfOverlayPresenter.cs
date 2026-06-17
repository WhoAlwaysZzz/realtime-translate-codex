using System.Windows;
using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Presentation;

namespace RealtimeTranslate.App.Presentation;

public sealed class WpfOverlayPresenter : IOverlayPresenter
{
    private readonly List<LookupOverlayWindow> _windows = [];
    private int _nextSlot;

    public void Show(LookupPresentation presentation)
    {
        var window = new LookupOverlayWindow(presentation);
        window.Closed += (_, _) => _windows.Remove(window);
        _windows.Add(window);

        var slot = _nextSlot++ % 6;
        window.Left = SystemParameters.WorkArea.Left + (SystemParameters.WorkArea.Width - 360) / 2;
        window.Top = SystemParameters.WorkArea.Top + 24 + slot * 86;
        window.Show();
    }
}
