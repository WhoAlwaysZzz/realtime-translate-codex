using Forms = System.Windows.Forms;

namespace RealtimeTranslate.App.SystemIntegration;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayIconService(Action selectWord, Action openArchive, Action exit)
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("Select word (Ctrl+Shift+X)", null, (_, _) => selectWord());
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
