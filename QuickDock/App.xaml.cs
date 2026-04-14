using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using QuickDock.Models;
using QuickDock.Services;
using QuickDock.Windows;

namespace QuickDock;

public partial class App : System.Windows.Application
{
    private ConfigService? _configService;
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
        _configService.SettingsChanged += OnSettingsChanged;
        _autoStartService = new AutoStartService();
        
        if (_configService.Settings.AutoStart)
        {
            _autoStartService.Enable();
        }

        _hotZoneService = new HotZoneService(
            _configService.Settings.HotZoneWidth,
            _configService.Settings.HotZoneTriggerDelay,
            _configService.Settings.HotZoneEdgeSize);
        _hotZoneService.HotZoneEntered += OnHotZoneEntered;
        _hotZoneService.HotZoneLeft += OnHotZoneLeft;
        _hotZoneService.Start();

        _mainWindow = new MainWindow(_configService);
        _mainWindow.MouseLeave += OnMainWindowMouseLeave;

        CreateTaskbarIcon();

        _mainWindow.ShowStartupAnimation();
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

        var contextMenu = new System.Windows.Controls.ContextMenu();
        
        var showItem = new System.Windows.Controls.MenuItem { Header = Lang.T("Tray.Show") };
        showItem.Click += (s, e) => ShowDock();
        contextMenu.Items.Add(showItem);

        var settingsItem = new System.Windows.Controls.MenuItem { Header = Lang.T("Tray.Settings") };
        settingsItem.Click += (s, e) => OpenSettings();
        contextMenu.Items.Add(settingsItem);

        contextMenu.Items.Add(new System.Windows.Controls.Separator());

        var exitItem = new System.Windows.Controls.MenuItem { Header = Lang.T("Tray.Exit") };
        exitItem.Click += (s, e) => Shutdown();
        contextMenu.Items.Add(exitItem);

        _taskbarIcon.ContextMenu = contextMenu;
        _taskbarIcon.TrayMouseDoubleClick += (s, e) => ShowDock();
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
        });
    }

    private void OnHotZoneLeft()
    {
    }

    private void OnMainWindowMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_mainWindow?.IsAutoHideSuppressed == true)
        {
            return;
        }

        _mainWindow?.SlideOut();
    }

    private void ShowDock()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.SlideIn();
            _mainWindow.Activate();
        }
    }

    private void OpenSettings()
    {
        try
        {
            var settingsWindow = new SettingsWindow(_configService!, _autoStartService!);
            if (settingsWindow.ShowDialog() == true)
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

    private void UpdateTrayMenuLanguage()
    {
        if (_taskbarIcon?.ContextMenu is System.Windows.Controls.ContextMenu menu)
        {
            if (menu.Items[0] is System.Windows.Controls.MenuItem showItem)
                showItem.Header = Lang.T("Tray.Show");
            if (menu.Items[1] is System.Windows.Controls.MenuItem settingsItem)
                settingsItem.Header = Lang.T("Tray.Settings");
            if (menu.Items[3] is System.Windows.Controls.MenuItem exitItem)
                exitItem.Header = Lang.T("Tray.Exit");
            _taskbarIcon.ToolTipText = Lang.T("Tray.ToolTip");
        }
    }

    private void OnSettingsChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(AppSettings.Language):
                UpdateTrayMenuLanguage();
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
            _configService.Settings.HotZoneWidth,
            _configService.Settings.HotZoneTriggerDelay,
            _configService.Settings.HotZoneEdgeSize);
        _hotZoneService.HotZoneEntered += OnHotZoneEntered;
        _hotZoneService.HotZoneLeft += OnHotZoneLeft;
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
