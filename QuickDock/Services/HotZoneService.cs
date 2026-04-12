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

    private const int VK_LBUTTON = 0x01;

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

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    private readonly System.Windows.Threading.DispatcherTimer _timer;
    private readonly double _hotZoneWidthRatio;
    private readonly int _triggerDelayMs;
    private readonly int _edgeSize;
    private bool _isInHotZone;
    private bool _pendingTrigger;
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

        if (isInHotNow)
        {
            if (_isInHotZone)
                return;

            if (!_isWaitingForDelay)
            {
                _isWaitingForDelay = true;
                _hotZoneEnterTime = DateTime.Now;
                return;
            }

            if (isLeftButtonDown)
            {
                _pendingTrigger = true;
                return;
            }

            var elapsed = (DateTime.Now - _hotZoneEnterTime).TotalMilliseconds;
            if (elapsed < _triggerDelayMs)
                return;

            if (_pendingTrigger)
                _pendingTrigger = false;

            _isWaitingForDelay = false;
            _isInHotZone = true;
            HotZoneEntered?.Invoke();
        }
        else
        {
            _pendingTrigger = false;
            _isWaitingForDelay = false;
            if (_isInHotZone)
            {
                _isInHotZone = false;
                HotZoneLeft?.Invoke();
            }
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
