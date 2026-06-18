using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using RealtimeTranslate.App.Settings;

namespace RealtimeTranslate.App.SystemIntegration;

public sealed class GlobalHotkeyService : IDisposable
{
    private const int HotkeyId = 0x1999;
    private const int WmHotkey = 0x0312;
    private const uint ModAlt = 0x0001;
    private const uint ModControl = 0x0002;
    private const uint ModShift = 0x0004;
    private const uint ModWin = 0x0008;

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

    public HotkeyGesture? RegisteredHotkey { get; private set; }

    public void Register(HotkeyGesture hotkey)
    {
        Unregister();

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key);
        _registered = RegisterHotKey(_source.Handle, HotkeyId, ToNativeModifiers(hotkey.Modifiers), virtualKey);
        if (!_registered)
        {
            throw new InvalidOperationException($"Unable to register {hotkey} global hotkey.");
        }

        RegisteredHotkey = hotkey;
    }

    public void Unregister()
    {
        if (_registered)
        {
            UnregisterHotKey(_source.Handle, HotkeyId);
            _registered = false;
        }

        RegisteredHotkey = null;
    }

    public void Dispose()
    {
        Unregister();
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

    private static uint ToNativeModifiers(ModifierKeys modifiers)
    {
        var native = 0u;
        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            native |= ModAlt;
        }

        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            native |= ModControl;
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            native |= ModShift;
        }

        if (modifiers.HasFlag(ModifierKeys.Windows))
        {
            native |= ModWin;
        }

        return native;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
