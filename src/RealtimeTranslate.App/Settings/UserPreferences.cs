namespace RealtimeTranslate.App.Settings;

public sealed record UserPreferences(HotkeyGesture Hotkey, SelectionMode SelectionMode)
{
    public static UserPreferences Default { get; } = new(HotkeyGesture.Default, SelectionMode.OneShot);
}
