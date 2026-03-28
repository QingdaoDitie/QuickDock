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
    private bool _isInHotZone;
    private bool _disposed;

    public event Action? HotZoneEntered;
    public event Action? HotZoneLeft;

    public HotZoneService(double hotZoneWidthRatio = 0.3, int checkInterval = 50)
    {
        _hotZoneWidthRatio = hotZoneWidthRatio;
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

        var isInHotNow = point.Y <= screen.Top + 5 &&
                         point.X >= hotZoneLeft &&
                         point.X <= hotZoneRight;

        if (isInHotNow && !_isInHotZone)
        {
            _isInHotZone = true;
            HotZoneEntered?.Invoke();
        }
        else if (!isInHotNow && _isInHotZone)
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
