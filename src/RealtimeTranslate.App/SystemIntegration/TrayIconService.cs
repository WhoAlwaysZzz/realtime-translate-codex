using Forms = System.Windows.Forms;
using RealtimeTranslate.App.Settings;
using AppSelectionMode = RealtimeTranslate.App.Settings.SelectionMode;

namespace RealtimeTranslate.App.SystemIntegration;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ToolStripMenuItem _selectWordItem;
    private readonly Forms.ToolStripMenuItem _manualLookupItem;
    private readonly Forms.ToolStripMenuItem _recentLookupsItem;
    private readonly Forms.ToolStripMenuItem _explainSelectionItem;
    private readonly Forms.ToolStripMenuItem _configureHotkeyItem;
    private readonly Forms.ToolStripMenuItem _configureManualHotkeyItem;
    private readonly Forms.ToolStripMenuItem _configureRecentLookupsHotkeyItem;
    private readonly Forms.ToolStripMenuItem _configureExplainSelectionHotkeyItem;
    private readonly Forms.ToolStripMenuItem _oneShotModeItem;
    private readonly Forms.ToolStripMenuItem _continuousModeItem;

    public TrayIconService(
        Action selectWord,
        Action manualLookup,
        Action showRecentLookups,
        Action explainSelection,
        Action configureHotkey,
        Action configureManualHotkey,
        Action configureRecentLookupsHotkey,
        Action configureExplainSelectionHotkey,
        Action<AppSelectionMode> setSelectionMode,
        Action openArchive,
        Action exit)
    {
        var menu = new Forms.ContextMenuStrip();
        _selectWordItem = new Forms.ToolStripMenuItem("Select word", null, (_, _) => selectWord());
        _manualLookupItem = new Forms.ToolStripMenuItem("Manual lookup", null, (_, _) => manualLookup());
        _recentLookupsItem = new Forms.ToolStripMenuItem("Show recent lookups", null, (_, _) => showRecentLookups());
        _explainSelectionItem = new Forms.ToolStripMenuItem("Explain selected text", null, (_, _) => explainSelection());
        _configureHotkeyItem = new Forms.ToolStripMenuItem("Configure hotkey...", null, (_, _) => configureHotkey());
        _configureManualHotkeyItem = new Forms.ToolStripMenuItem("Configure manual lookup hotkey...", null, (_, _) => configureManualHotkey());
        _configureRecentLookupsHotkeyItem = new Forms.ToolStripMenuItem("Configure recent lookups hotkey...", null, (_, _) => configureRecentLookupsHotkey());
        _configureExplainSelectionHotkeyItem = new Forms.ToolStripMenuItem("Configure explanation hotkey...", null, (_, _) => configureExplainSelectionHotkey());
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
        menu.Items.Add(_recentLookupsItem);
        menu.Items.Add(_explainSelectionItem);
        menu.Items.Add(_configureHotkeyItem);
        menu.Items.Add(_configureManualHotkeyItem);
        menu.Items.Add(_configureRecentLookupsHotkeyItem);
        menu.Items.Add(_configureExplainSelectionHotkeyItem);
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
        _recentLookupsItem.Text = $"Show recent lookups ({preferences.RecentLookupsHotkey})";
        _configureRecentLookupsHotkeyItem.Text = $"Configure recent lookups hotkey... Current: {preferences.RecentLookupsHotkey}";
        _explainSelectionItem.Text = $"Explain selected text ({preferences.ExplainSelectionHotkey})";
        _configureExplainSelectionHotkeyItem.Text = $"Configure explanation hotkey... Current: {preferences.ExplainSelectionHotkey}";
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
