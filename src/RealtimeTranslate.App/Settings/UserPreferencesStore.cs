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

            var hotkey = ParseHotkey(dto.HotkeyKey, dto.HotkeyModifiers, HotkeyGesture.Default);
            var manualHotkey = ParseHotkey(dto.ManualLookupHotkeyKey, dto.ManualLookupHotkeyModifiers, UserPreferences.Default.ManualLookupHotkey);
            var recentHotkey = ParseHotkey(dto.RecentLookupsHotkeyKey, dto.RecentLookupsHotkeyModifiers, UserPreferences.Default.RecentLookupsHotkey);
            var explainHotkey = ParseHotkey(dto.ExplainSelectionHotkeyKey, dto.ExplainSelectionHotkeyModifiers, UserPreferences.Default.ExplainSelectionHotkey);
            var mode = ParseEnum(dto.SelectionMode, SelectionMode.OneShot);

            return new UserPreferences(
                hotkey,
                manualHotkey,
                recentHotkey,
                explainHotkey,
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
            preferences.RecentLookupsHotkey.Key.ToString(),
            preferences.RecentLookupsHotkey.Modifiers.ToString(),
            preferences.ExplainSelectionHotkey.Key.ToString(),
            preferences.ExplainSelectionHotkey.Modifiers.ToString(),
            preferences.SelectionMode.ToString());
        File.WriteAllText(_path, JsonSerializer.Serialize(dto, JsonOptions));
    }

    private static HotkeyGesture ParseHotkey(string? keyValue, string? modifiersValue, HotkeyGesture fallback)
    {
        var key = ParseEnum(keyValue, fallback.Key);
        var modifiers = ParseEnum(modifiersValue, fallback.Modifiers);
        var hotkey = new HotkeyGesture(key, modifiers);
        return hotkey.IsValid ? hotkey : fallback;
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct
    {
        return Enum.TryParse<TEnum>(value, out var parsed)
            ? parsed
            : fallback;
    }

    private sealed record UserPreferencesDto(
        string? HotkeyKey,
        string? HotkeyModifiers,
        string? ManualLookupHotkeyKey,
        string? ManualLookupHotkeyModifiers,
        string? RecentLookupsHotkeyKey,
        string? RecentLookupsHotkeyModifiers,
        string? ExplainSelectionHotkeyKey,
        string? ExplainSelectionHotkeyModifiers,
        string? SelectionMode);
}
