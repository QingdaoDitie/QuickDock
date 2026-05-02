using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace QuickDock.Services;

public class AutoHideController
{
    private readonly Window _window;
    private readonly ConfigService _configService;
    private readonly FrameworkElement _mainGrid;
    private readonly Func<bool> _isHiddenFunc;
    private readonly Func<bool> _isAnimatingFunc;
    private readonly Func<bool> _isAutoHideSuppressedFunc;
    private readonly Action _slideOutAction;

    private readonly DispatcherTimer _mouseCheckTimer;

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public AutoHideController(
        Window window,
        ConfigService configService,
        FrameworkElement mainGrid,
        Func<bool> isHiddenFunc,
        Func<bool> isAnimatingFunc,
        Func<bool> isAutoHideSuppressedFunc,
        Action slideOutAction)
    {
        _window = window;
        _configService = configService;
        _mainGrid = mainGrid;
        _isHiddenFunc = isHiddenFunc;
        _isAnimatingFunc = isAnimatingFunc;
        _isAutoHideSuppressedFunc = isAutoHideSuppressedFunc;
        _slideOutAction = slideOutAction;

        _mouseCheckTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _mouseCheckTimer.Tick += OnMouseCheckTimerTick;
    }

    public void Start()
    {
        _mouseCheckTimer.Start();
    }

    public void Stop()
    {
        _mouseCheckTimer.Stop();
    }

    public void OnMouseLeave(WpfMouseEventArgs e)
    {
        if (_isAutoHideSuppressedFunc())
        {
            return;
        }

        if (!_isAnimatingFunc() && !_isHiddenFunc())
        {
            var position = e.GetPosition(_mainGrid);
            if (position.Y > _mainGrid.ActualHeight || position.X < 0 || position.X > _mainGrid.ActualWidth)
            {
                _slideOutAction();
            }
        }
    }

    private void OnMouseCheckTimerTick(object? sender, EventArgs e)
    {
        if (_isHiddenFunc() || _isAnimatingFunc())
        {
            _mouseCheckTimer.Stop();
            return;
        }

        if (_isAutoHideSuppressedFunc())
        {
            return;
        }

        if (System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            return;

        if (!GetCursorPos(out POINT point))
            return;

        var dpiScale = VisualTreeHelper.GetDpi(_window);
        var windowLeft = _window.Left * dpiScale.DpiScaleX;
        var windowRight = (_window.Left + _window.ActualWidth) * dpiScale.DpiScaleX;
        var windowTop = 0;
        var windowBottom = (_window.ActualHeight * _configService.Settings.Scale) * dpiScale.DpiScaleY;

        var tolerance = _configService.Settings.AutoHideTolerance;
        var isMouseOverWindow = point.X >= windowLeft && point.X <= windowRight &&
                                point.Y >= windowTop && point.Y <= windowBottom + tolerance;

        if (!isMouseOverWindow)
        {
            _slideOutAction();
        }
    }
}
