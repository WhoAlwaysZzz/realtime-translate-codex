using System.Windows.Input;

namespace RealtimeTranslate.App.Settings;

public static class HotkeyFallbackPolicy
{
    public static IEnumerable<HotkeyGesture> GetCandidates(HotkeyGesture preferred)
    {
        var seen = new HashSet<HotkeyGesture>();

        foreach (var candidate in EnumerateCandidates(preferred))
        {
            if (candidate.IsValid && seen.Add(candidate))
            {
                yield return candidate;
            }
        }
    }

    private static IEnumerable<HotkeyGesture> EnumerateCandidates(HotkeyGesture preferred)
    {
        yield return preferred;

        if (preferred != HotkeyGesture.Default)
        {
            yield return HotkeyGesture.Default;
        }

        if (preferred.Modifiers is not ModifierKeys.None)
        {
            for (var key = Key.A; key <= Key.Z; key++)
            {
                yield return new HotkeyGesture(key, preferred.Modifiers);
            }
        }

        for (var key = Key.F7; key <= Key.F24; key++)
        {
            yield return new HotkeyGesture(key, ModifierKeys.None);
        }

        for (var key = Key.F1; key <= Key.F6; key++)
        {
            yield return new HotkeyGesture(key, ModifierKeys.None);
        }
    }
}
