using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using QuickDock.Models;

namespace QuickDock.Services;

internal sealed class PopupPolicy(AppSettings settings)
{
    private const int D3dExclusiveFullscreen = 3;
    private const int ExtendedFrameBounds = 9;
    private const uint MonitorDefaultToNearest = 2;

    private uint _lastProcessId;
    private bool _lastProcessIsGame;
    private long _nextProcessCheck;

    public bool IsSuppressed => settings.PopupMode switch
    {
        PopupMode.DoNotDisturb => true,
        PopupMode.Game => IsFullscreenGameInForeground(),
        _ => false
    };

    private bool IsFullscreenGameInForeground()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || GetWindowThreadProcessId(hwnd, out var processId) == 0 ||
            processId == Environment.ProcessId)
        {
            return false;
        }

        var monitor = MonitorFromWindow(hwnd, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info) ||
            DwmGetWindowAttribute(hwnd, ExtendedFrameBounds, out var bounds, Marshal.SizeOf<Rect>()) != 0)
        {
            return false;
        }

        if (bounds.Left > info.Bounds.Left || bounds.Top > info.Bounds.Top ||
            bounds.Right < info.Bounds.Right || bounds.Bottom < info.Bounds.Bottom)
        {
            return false;
        }

        return (SHQueryUserNotificationState(out var state) == 0 && state == D3dExclusiveFullscreen) ||
               IsGameProcess(processId);
    }

    private bool IsGameProcess(uint processId)
    {
        var now = Environment.TickCount64;
        if (_lastProcessId == processId && now < _nextProcessCheck)
        {
            return _lastProcessIsGame;
        }

        _lastProcessId = processId;
        _nextProcessCheck = now + 2000;
        _lastProcessIsGame = false;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            var path = process.MainModule?.FileName;
            if (path != null)
            {
                _lastProcessIsGame =
                    path.Contains(@"\steamapps\common\", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(@"\XboxGames\", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(@"\Epic Games\", StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(@"\GOG Galaxy\Games\", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetFileNameWithoutExtension(path).EndsWith("-Shipping", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {
            // The foreground process may have exited or may restrict module access.
        }

        return _lastProcessIsGame;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public Rect Bounds;
        public Rect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out Rect bounds, int size);

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out int state);
}
