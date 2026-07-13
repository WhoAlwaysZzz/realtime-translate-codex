using System.Runtime.InteropServices;
using System.Windows;
using RealtimeTranslate.App.Selection;
using RealtimeTranslate.Core.Models;
using WpfApplication = System.Windows.Application;
using WpfPoint = System.Windows.Point;

namespace RealtimeTranslate.App.SystemIntegration;

public sealed class GlobalRightDragSelectionService : IDisposable
{
    private const int WhMouseLl = 14;
    private const int WmMouseMove = 0x0200;
    private const int WmRButtonDown = 0x0204;
    private const int WmRButtonUp = 0x0205;
    private const int DragThresholdPixels = 10;

    private readonly LowLevelMouseProc _proc;
    private IntPtr _hook;
    private WpfPoint? _start;
    private bool _dragging;
    private PassiveSelectionOverlayWindow? _overlay;

    public GlobalRightDragSelectionService()
    {
        _proc = HookCallback;
    }

    public event EventHandler<ScreenSelection>? SelectionCompleted;

    public void Start()
    {
        if (_hook != IntPtr.Zero)
        {
            return;
        }

        _hook = SetWindowsHookEx(WhMouseLl, _proc, IntPtr.Zero, 0);
        if (_hook == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to install right-drag mouse hook.");
        }
    }

    public void Stop()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }

        ResetDragState();
        CloseOverlay();
    }

    public void Dispose()
    {
        Stop();
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        var hook = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
        var message = wParam.ToInt32();
        var point = new WpfPoint(hook.pt.x, hook.pt.y);

        switch (message)
        {
            case WmRButtonDown:
                _start = point;
                _dragging = false;
                break;

            case WmMouseMove:
                if (_start is not null)
                {
                    if (!_dragging && GetDistance(_start.Value, point) >= DragThresholdPixels)
                    {
                        _dragging = true;
                        ShowOverlay();
                    }

                    if (_dragging)
                    {
                        UpdateOverlay(ToSelection(_start.Value, point));
                    }
                }
                break;

            case WmRButtonUp:
                if (_start is not null)
                {
                    var wasDragging = _dragging;
                    var selection = ToSelection(_start.Value, point);
                    ResetDragState();

                    if (wasDragging)
                    {
                        CompleteSelection(selection);
                    }
                }
                break;
        }

        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private void ShowOverlay()
    {
        WpfApplication.Current.Dispatcher.BeginInvoke(() =>
        {
            _overlay ??= new PassiveSelectionOverlayWindow();
            if (!_overlay.IsVisible)
            {
                _overlay.Show();
            }
        });
    }

    private void UpdateOverlay(ScreenSelection selection)
    {
        WpfApplication.Current.Dispatcher.BeginInvoke(() =>
        {
            _overlay?.UpdateSelection(selection);
        });
    }

    private void CompleteSelection(ScreenSelection selection)
    {
        WpfApplication.Current.Dispatcher.BeginInvoke(async () =>
        {
            CloseOverlay();
            if (!selection.IsEmpty)
            {
                await Task.Delay(120);
                SelectionCompleted?.Invoke(this, selection);
            }
        });
    }

    private void ResetDragState()
    {
        _start = null;
        _dragging = false;
    }

    private void CloseOverlay()
    {
        WpfApplication.Current.Dispatcher.BeginInvoke(() =>
        {
            _overlay?.Close();
            _overlay = null;
        });
    }

    private static ScreenSelection ToSelection(WpfPoint start, WpfPoint end)
    {
        var x = (int)Math.Round(Math.Min(start.X, end.X));
        var y = (int)Math.Round(Math.Min(start.Y, end.Y));
        var width = (int)Math.Round(Math.Abs(start.X - end.X));
        var height = (int)Math.Round(Math.Abs(start.Y - end.Y));
        return new ScreenSelection(x, y, width, height);
    }

    private static double GetDistance(WpfPoint start, WpfPoint end)
    {
        var dx = start.X - end.X;
        var dy = start.Y - end.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct POINT
    {
        public readonly int x;
        public readonly int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct MSLLHOOKSTRUCT
    {
        public readonly POINT pt;
        public readonly uint mouseData;
        public readonly uint flags;
        public readonly uint time;
        public readonly IntPtr dwExtraInfo;
    }
}
