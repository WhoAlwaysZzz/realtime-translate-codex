using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace RealtimeTranslate.App.Settings;

public sealed class UserPreferencesStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _path;

    public UserPreferencesStore(string path)
    {
        _path = path;
    }

    public UserPreferences Load()
    {
        if (!File.Exists(_path))
        {
            return UserPreferences.Default;
        }

        try
        {
            var json = File.ReadAllText(_path);
            var dto = JsonSerializer.Deserialize<UserPreferencesDto>(json, JsonOptions);
            if (dto is null)
            {
                return UserPreferences.Default;
            }

            var key = Enum.TryParse<Key>(dto.HotkeyKey, out var parsedKey) ? parsedKey : HotkeyGesture.Default.Key;
            var modifiers = Enum.TryParse<ModifierKeys>(dto.HotkeyModifiers, out var parsedModifiers)
                ? parsedModifiers
                : HotkeyGesture.Default.Modifiers;
            var manualKey = Enum.TryParse<Key>(dto.ManualLookupHotkeyKey, out var parsedManualKey)
                ? parsedManualKey
                : UserPreferences.Default.ManualLookupHotkey.Key;
            var manualModifiers = Enum.TryParse<ModifierKeys>(dto.ManualLookupHotkeyModifiers, out var parsedManualModifiers)
                ? parsedManualModifiers
                : UserPreferences.Default.ManualLookupHotkey.Modifiers;
            var mode = Enum.TryParse<SelectionMode>(dto.SelectionMode, out var parsedMode)
                ? parsedMode
                : SelectionMode.OneShot;
            var hotkey = new HotkeyGesture(key, modifiers);
            var manualHotkey = new HotkeyGesture(manualKey, manualModifiers);

            return new UserPreferences(
                hotkey.IsValid ? hotkey : HotkeyGesture.Default,
                manualHotkey.IsValid ? manualHotkey : UserPreferences.Default.ManualLookupHotkey,
                mode);
        }
        catch
        {
            return UserPreferences.Default;
        }
    }

    public void Save(UserPreferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var dto = new UserPreferencesDto(
            preferences.Hotkey.Key.ToString(),
            preferences.Hotkey.Modifiers.ToString(),
            preferences.ManualLookupHotkey.Key.ToString(),
            preferences.ManualLookupHotkey.Modifiers.ToString(),
            preferences.SelectionMode.ToString());
        File.WriteAllText(_path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    private sealed record UserPreferencesDto(
        string? HotkeyKey,
        string? HotkeyModifiers,
        string? ManualLookupHotkeyKey,
        string? ManualLookupHotkeyModifiers,
        string? SelectionMode);
}
