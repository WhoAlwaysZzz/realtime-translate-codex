namespace RealtimeTranslate.App.Settings;

public sealed record UserPreferences(HotkeyGesture Hotkey, HotkeyGesture ManualLookupHotkey, SelectionMode SelectionMode)
{
    public static UserPreferences Default { get; } = new(
        HotkeyGesture.Default,
        new HotkeyGesture(System.Windows.Input.Key.F4, System.Windows.Input.ModifierKeys.None),
        SelectionMode.OneShot);
}
