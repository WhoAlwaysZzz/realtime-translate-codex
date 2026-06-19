using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Presentation;

namespace RealtimeTranslate.App.Presentation;

public sealed class WpfTextExplanationPresenter : ITextExplanationPresenter
{
    public void Show(TextExplanationPresentation presentation)
    {
        var window = new TextExplanationWindow(presentation);
        window.Show();
        window.Activate();
    }
}
