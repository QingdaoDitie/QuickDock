using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using Hardcodet.Wpf.TaskbarNotification;
using QuickDock.Models;
using QuickDock.Services;
using QuickDock.Windows;
using WpfMenuItem = System.Windows.Controls.MenuItem;

namespace QuickDock;

public partial class App : System.Windows.Application
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private ConfigService? _configService;
    private PopupPolicy? _popupPolicy;
    private HotZoneService? _hotZoneService;
    private MainWindow? _mainWindow;
    private TaskbarIcon? _taskbarIcon;
    private AutoStartService? _autoStartService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        _configService = new ConfigService();
        _popupPolicy = new PopupPolicy(_configService.Settings);
        _configService.SettingsChanged += OnSettingsChanged;
        _autoStartService = new AutoStartService();
        
        if (_configService.Settings.AutoStart)
        {
            _autoStartService.Enable();
        }

        _mainWindow = new MainWindow(_configService, _popupPolicy);
        _mainWindow.MouseLeave += OnMainWindowMouseLeave;

        RecreateHotZoneService();
        CreateTaskbarIcon();

        if (_configService.IsFirstRun)
        {
            Dispatcher.InvokeAsync(OpenSettings, DispatcherPriority.Loaded);
        }
        else
        {
            _mainWindow.ShowStartupAnimation();
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            var message = $"发生错误: {e.Exception.Message}\n\n{e.Exception.StackTrace}";
            System.Windows.MessageBox.Show(message, "QuickDock Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch { }
        e.Handled = true;
    }

    private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
            {
                var message = $"严重错误: {ex.Message}\n\n{ex.StackTrace}";
                System.Windows.MessageBox.Show(message, "QuickDock Critical Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch { }
    }

    private void CreateTaskbarIcon()
    {
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "app.ico");
        System.Drawing.Icon icon;
        
        if (System.IO.File.Exists(iconPath))
        {
            icon = new System.Drawing.Icon(iconPath);
        }
        else
        {
            icon = System.Drawing.SystemIcons.Application;
        }
        
        _taskbarIcon = new TaskbarIcon
        {
            Icon = icon,
            ToolTipText = Lang.T("Tray.ToolTip")
        };

        UpdateTrayMenu();
        _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowDock();
    }

    private void UpdateTrayMenu()
    {
        if (_taskbarIcon == null || _configService == null)
        {
            return;
        }

        var config = _configService;
        var contextMenu = new System.Windows.Controls.ContextMenu();

        var showItem = new WpfMenuItem { Header = Lang.T("Tray.Show") };
        showItem.Click += (s, e) => ShowDock();
        contextMenu.Items.Add(showItem);

        var modeMenu = new WpfMenuItem { Header = Lang.T("Settings.PopupMode") };
        foreach (var mode in Enum.GetValues<PopupMode>())
        {
            var modeItem = new WpfMenuItem
            {
                Header = Lang.T($"Settings.PopupMode.{mode}"),
                IsCheckable = true
            };
            contextMenu.Opened += (_, _) => modeItem.IsChecked = config.Settings.PopupMode == mode;
            modeItem.Click += (_, _) =>
            {
                modeItem.IsChecked = true;
                if (config.Settings.PopupMode != mode)
                {
                    config.Settings.PopupMode = mode;
                    config.Save();
                }
            };
            modeMenu.Items.Add(modeItem);
        }
        contextMenu.Items.Add(modeMenu);

        var settingsItem = new WpfMenuItem { Header = Lang.T("Tray.Settings") };
        settingsItem.Click += (s, e) => OpenSettings();
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new WpfMenuItem { Header = Lang.T("Tray.Exit") };
        exitItem.Click += (s, e) => Shutdown();
        contextMenu.Items.Add(exitItem);

        _taskbarIcon.ContextMenu = contextMenu;
        _taskbarIcon.ToolTipText = Lang.T("Tray.ToolTip");
    }

    private void OnHotZoneEntered()
    {
        Dispatcher.Invoke(() =>
        {
            if (_mainWindow != null && !_mainWindow.IsVisible)
            {
                _mainWindow.Show();
                _mainWindow.Activate();
            }
            _mainWindow?.SlideIn();
            _mainWindow?.ShowLatestWidgetNotificationOnDockTouch();
        });
    }

    private bool SuppressPopupsIfNeeded()
    {
        if (_popupPolicy?.IsSuppressed != true)
        {
            return false;
        }

        _mainWindow?.DismissPopups();
        return true;
    }

    private void OnMainWindowMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_mainWindow?.IsAutoHideSuppressed == true)
        {
            return;
        }

        if (_mainWindow != null && IsCursorWithinWindowBounds(_mainWindow))
        {
            return;
        }

        _mainWindow?.SlideOut();
    }

    private bool IsCursorWithinWindowBounds(Window window)
    {
        if (!GetCursorPos(out POINT point))
        {
            return false;
        }

        var dpi = VisualTreeHelper.GetDpi(window);
        var tolerance = _configService?.Settings.AutoHideTolerance ?? 10;
        var left = window.Left * dpi.DpiScaleX - tolerance;
        var top = window.Top * dpi.DpiScaleY - tolerance;
        var right = (window.Left + window.ActualWidth) * dpi.DpiScaleX + tolerance;
        var bottom = (window.Top + window.ActualHeight) * dpi.DpiScaleY + tolerance;

        return point.X >= left && point.X <= right &&
               point.Y >= top && point.Y <= bottom;
    }

    private void ShowDock()
    {
        if (_mainWindow != null && !SuppressPopupsIfNeeded())
        {
            _mainWindow.Show();
            _mainWindow.SlideIn();
            _mainWindow.ShowLatestWidgetNotificationOnDockTouch();
            _mainWindow.Activate();
        }
    }

    private void OpenSettings()
    {
        try
        {
            var settingsWindow = _mainWindow?.CreateSettingsWindow(_autoStartService!);
            if (settingsWindow?.ShowDialog() == true)
            {
                _mainWindow?.RefreshItems();
                _mainWindow?.RefreshTools();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"打开设置失败: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSettingsChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(AppSettings.Language):
                UpdateTrayMenu();
                break;
            case nameof(AppSettings.AutoStart):
                if (_configService?.Settings.AutoStart == true)
                {
                    _autoStartService?.Enable();
                }
                else
                {
                    _autoStartService?.Disable();
                }
                break;
            case nameof(AppSettings.HotZoneWidth):
            case nameof(AppSettings.HotZoneTriggerDelay):
            case nameof(AppSettings.HotZoneEdgeSize):
                RecreateHotZoneService();
                break;
            case nameof(AppSettings.PopupMode):
                SuppressPopupsIfNeeded();
                break;
        }
    }

    private void RecreateHotZoneService()
    {
        if (_configService == null)
        {
            return;
        }

        _hotZoneService?.Stop();
        _hotZoneService?.Dispose();

        _hotZoneService = new HotZoneService(
            SuppressPopupsIfNeeded,
            _configService.Settings.HotZoneWidth,
            _configService.Settings.HotZoneTriggerDelay,
            _configService.Settings.HotZoneEdgeSize);
        _hotZoneService.HotZoneEntered += OnHotZoneEntered;
        _hotZoneService.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_configService != null)
        {
            _configService.SettingsChanged -= OnSettingsChanged;
        }
        _hotZoneService?.Dispose();
        _taskbarIcon?.Dispose();
        base.OnExit(e);
    }
}
