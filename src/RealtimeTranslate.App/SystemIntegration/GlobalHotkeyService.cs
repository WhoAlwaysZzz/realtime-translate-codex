using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace RealtimeTranslate.App.SystemIntegration;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0x1999;
    private const int WmHotkey = 0x0312;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;

    private readonly HwndSource _source;
    private bool _registered;

    public GlobalHotkeyService()
    {
        _source = new HwndSource(new HwndSourceParameters("RealtimeTranslateCodexHotkey")
        {
            Width = 0,
            Height = 0,
            WindowStyle = unchecked((int)0x80000000),
        });
        _source.AddHook(WndProc);
    }

    public event EventHandler? Pressed;

    public void Register()
    {
        if (_registered)
        {
            return;
        }

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(Key.X);
        _registered = RegisterHotKey(_source.Handle, HotkeyId, ModControl | ModShift, virtualKey);
        if (!_registered)
        {
            throw new InvalidOperationException("Unable to register Ctrl+Shift+X global hotkey.");
        }
    }

    public void Dispose()
    {
        if (_registered)
        {
            UnregisterHotKey(_source.Handle, HotkeyId);
        }

        _source.RemoveHook(WndProc);
        _source.Dispose();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmHotkey && wParam.ToInt32() == HotkeyId)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
