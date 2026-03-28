using System.Windows;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
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
        _autoStartService = new AutoStartService();
        
        if (_configService.Settings.AutoStart)
        {
            _autoStartService.Enable();
        }

        _hotZoneService = new HotZoneService(_configService.Settings.HotZoneWidth);
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
            settingsWindow.ShowDialog();
            
            if (_hotZoneService != null && _configService != null)
            {
                _hotZoneService.Stop();
                var newService = new HotZoneService(_configService.Settings.HotZoneWidth);
                newService.HotZoneEntered += OnHotZoneEntered;
                newService.HotZoneLeft += OnHotZoneLeft;
                newService.Start();
                _hotZoneService = newService;
            }
            
            _mainWindow?.RefreshSettings();
            UpdateTrayMenuLanguage();
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

    protected override void OnExit(ExitEventArgs e)
    {
        _hotZoneService?.Dispose();
        _taskbarIcon?.Dispose();
        base.OnExit(e);
    }
}
