using Forms = System.Windows.Forms;
using RealtimeTranslate.App.Settings;
using AppSelectionMode = RealtimeTranslate.App.Settings.SelectionMode;

namespace RealtimeTranslate.App.SystemIntegration;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _selectWordItem;
    private readonly Forms.ToolStripMenuItem _manualLookupItem;
    private readonly Forms.ToolStripMenuItem _configureHotkeyItem;
    private readonly Forms.ToolStripMenuItem _configureManualHotkeyItem;
    private readonly Forms.ToolStripMenuItem _oneShotModeItem;
    private readonly Forms.ToolStripMenuItem _continuousModeItem;

    public TrayIconService(
        Action selectWord,
        Action manualLookup,
        Action configureHotkey,
        Action configureManualHotkey,
        Action<AppSelectionMode> setSelectionMode,
        Action openArchive,
        Action exit)
    {
        var menu = new Forms.ContextMenuStrip();
        _selectWordItem = new Forms.ToolStripMenuItem("Select word", null, (_, _) => selectWord());
        _manualLookupItem = new Forms.ToolStripMenuItem("Manual lookup", null, (_, _) => manualLookup());
        _configureHotkeyItem = new Forms.ToolStripMenuItem("Configure hotkey...", null, (_, _) => configureHotkey());
        _configureManualHotkeyItem = new Forms.ToolStripMenuItem("Configure manual lookup hotkey...", null, (_, _) => configureManualHotkey());
        _oneShotModeItem = new Forms.ToolStripMenuItem("One-shot selection", null, (_, _) => setSelectionMode(AppSelectionMode.OneShot))
        {
            CheckOnClick = false,
        };
        _continuousModeItem = new Forms.ToolStripMenuItem("Continuous selection", null, (_, _) => setSelectionMode(AppSelectionMode.Continuous))
        {
            CheckOnClick = false,
        };

        menu.Items.Add(_selectWordItem);
        menu.Items.Add(_manualLookupItem);
        menu.Items.Add(_configureHotkeyItem);
        menu.Items.Add(_configureManualHotkeyItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(_oneShotModeItem);
        menu.Items.Add(_continuousModeItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Open archive folder", null, (_, _) => openArchive());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => exit());

        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Realtime Translate Codex",
            ContextMenuStrip = menu,
            Visible = true,
        };

        _notifyIcon.DoubleClick += (_, _) => selectWord();
    }

    public void UpdatePreferences(UserPreferences preferences, bool continuousModeActive)
    {
        _selectWordItem.Text = preferences.SelectionMode == AppSelectionMode.Continuous
            ? $"Toggle selection mode ({preferences.Hotkey})"
            : $"Select word ({preferences.Hotkey})";
        _configureHotkeyItem.Text = $"Configure hotkey... Current: {preferences.Hotkey}";
        _manualLookupItem.Text = $"Manual lookup ({preferences.ManualLookupHotkey})";
        _configureManualHotkeyItem.Text = $"Configure manual lookup hotkey... Current: {preferences.ManualLookupHotkey}";
        _oneShotModeItem.Checked = preferences.SelectionMode == AppSelectionMode.OneShot;
        _continuousModeItem.Checked = preferences.SelectionMode == AppSelectionMode.Continuous;
        _continuousModeItem.Text = continuousModeActive ? "Continuous selection (active)" : "Continuous selection";
    }

    public void ShowInfo(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(2500);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
