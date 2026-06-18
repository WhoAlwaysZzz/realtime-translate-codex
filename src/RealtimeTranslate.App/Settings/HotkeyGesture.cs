using System.Windows.Input;

namespace RealtimeTranslate.App.Settings;

public sealed record HotkeyGesture(Key Key, ModifierKeys Modifiers)
{
    public static HotkeyGesture Default { get; } = new(Key.X, ModifierKeys.Control | ModifierKeys.Shift);

    public bool IsValid => Key is not Key.None && (Modifiers is not ModifierKeys.None || IsFunctionKey(Key));

    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(ModifierKeys.Control))
        {
            parts.Add("Ctrl");
        }

        if (Modifiers.HasFlag(ModifierKeys.Alt))
        {
            parts.Add("Alt");
        }

        if (Modifiers.HasFlag(ModifierKeys.Shift))
        {
            parts.Add("Shift");
        }

        if (Modifiers.HasFlag(ModifierKeys.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }

    private static bool IsFunctionKey(Key key)
    {
        return key >= Key.F1 && key <= Key.F24;
    }
}
