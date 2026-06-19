namespace RealtimeTranslate.App.Settings;

public sealed record UserPreferences(
    HotkeyGesture Hotkey,
    HotkeyGesture ManualLookupHotkey,
    HotkeyGesture RecentLookupsHotkey,
    HotkeyGesture ExplainSelectionHotkey,
    SelectionMode SelectionMode)
{
    public static UserPreferences Default { get; } = new(
        HotkeyGesture.Default,
        new HotkeyGesture(System.Windows.Input.Key.F4, System.Windows.Input.ModifierKeys.None),
        new HotkeyGesture(System.Windows.Input.Key.F5, System.Windows.Input.ModifierKeys.None),
        new HotkeyGesture(System.Windows.Input.Key.F6, System.Windows.Input.ModifierKeys.None),
        SelectionMode.OneShot);

    public HotkeyGesture GetHotkey(LookupCommand command)
    {
        return command switch
        {
            LookupCommand.Selection => Hotkey,
            LookupCommand.ManualLookup => ManualLookupHotkey,
            LookupCommand.RecentLookups => RecentLookupsHotkey,
            LookupCommand.ExplainSelection => ExplainSelectionHotkey,
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null),
        };
    }

    public UserPreferences WithHotkey(LookupCommand command, HotkeyGesture hotkey)
    {
        return command switch
        {
            LookupCommand.Selection => this with { Hotkey = hotkey },
            LookupCommand.ManualLookup => this with { ManualLookupHotkey = hotkey },
            LookupCommand.RecentLookups => this with { RecentLookupsHotkey = hotkey },
            LookupCommand.ExplainSelection => this with { ExplainSelectionHotkey = hotkey },
            _ => throw new ArgumentOutOfRangeException(nameof(command), command, null),
        };
    }
}
