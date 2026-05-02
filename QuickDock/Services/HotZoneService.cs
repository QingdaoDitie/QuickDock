using System.Runtime.InteropServices;
using System.Windows;

namespace QuickDock.Services;

public class HotZoneService : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    private const int VK_LBUTTON = 0x01;
    private const int GUI_INMOVESIZE = 0x00000002;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public int dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public RECT rcCaret;
    }

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private readonly double _hotZoneWidthRatio;
    private readonly int _triggerDelayMs;
    private readonly int _edgeSize;
    private bool _isInHotZone;
    private bool _disposed;
    private DateTime _hotZoneEnterTime;
    private bool _isWaitingForDelay;

    public event Action? HotZoneEntered;
    public event Action? HotZoneLeft;

    public HotZoneService(double hotZoneWidthRatio = 0.3, int triggerDelayMs = 500, int edgeSize = 1, int checkInterval = 50)
    {
        _hotZoneWidthRatio = hotZoneWidthRatio;
        _triggerDelayMs = triggerDelayMs;
        _edgeSize = edgeSize;
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(checkInterval)
        };
        _timer.Tick += Timer_Tick;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!GetCursorPos(out POINT point))
            return;

        var screen = GetScreenBounds(point);
        var hotZoneWidth = (screen.Right - screen.Left) * _hotZoneWidthRatio;
        var hotZoneLeft = screen.Left + (screen.Right - screen.Left - hotZoneWidth) / 2;
        var hotZoneRight = hotZoneLeft + hotZoneWidth;

        var isInHotNow = point.Y <= screen.Top + _edgeSize &&
                         point.X >= hotZoneLeft &&
                         point.X <= hotZoneRight;

        var isLeftButtonDown = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;

        if (isLeftButtonDown && IsDraggingWindow())
        {
            ResetHotZoneState();
            return;
        }

        if (isInHotNow)
        {
            if (_isInHotZone)
                return;

            if (isLeftButtonDown)
            {
                _isWaitingForDelay = false;
                _isInHotZone = true;
                HotZoneEntered?.Invoke();
                return;
            }

            if (!_isWaitingForDelay)
            {
                _isWaitingForDelay = true;
                _hotZoneEnterTime = DateTime.Now;
                return;
            }

            var elapsed = (DateTime.Now - _hotZoneEnterTime).TotalMilliseconds;
            if (elapsed < _triggerDelayMs)
                return;

            _isWaitingForDelay = false;
            _isInHotZone = true;
            HotZoneEntered?.Invoke();
        }
        else
        {
            _isWaitingForDelay = false;
            if (_isInHotZone)
            {
                _isInHotZone = false;
                HotZoneLeft?.Invoke();
            }
        }
    }

    private bool IsDraggingWindow()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || !IsWindow(hwnd))
            return false;

        var threadId = GetWindowThreadProcessId(hwnd, IntPtr.Zero);
        if (threadId != 0)
        {
            var guiThreadInfo = new GUITHREADINFO
            {
                cbSize = Marshal.SizeOf<GUITHREADINFO>()
            };

            if (GetGUIThreadInfo(threadId, ref guiThreadInfo) &&
                (guiThreadInfo.flags & GUI_INMOVESIZE) != 0)
            {
                return true;
            }
        }

        if (!GetWindowRect(hwnd, out RECT rect))
            return false;

        if (!GetCursorPos(out POINT cursor))
            return false;

        var titleBarHeight = SystemParameters.CaptionHeight + 8;
        return cursor.Y >= rect.Top && cursor.Y <= rect.Top + titleBarHeight &&
               cursor.X >= rect.Left && cursor.X <= rect.Right;
    }

    private void ResetHotZoneState()
    {
        _isWaitingForDelay = false;
        if (_isInHotZone)
        {
            _isInHotZone = false;
            HotZoneLeft?.Invoke();
        }
    }

    private RECT GetScreenBounds(POINT point)
    {
        var monitor = MonitorFromPoint(point, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        GetMonitorInfo(monitor, ref info);
        return info.rcMonitor;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
