using System.Diagnostics;
using System.IO;
using System.Runtime.Loader;
using System.Windows;
using QuickDock.Controls;
using QuickDock.Models;
using QuickDock.Services;
using QuickDock.WidgetContracts;

namespace QuickDock.Widgets;

internal sealed class WidgetHost : IDisposable
{
    private readonly StatusControl _control;
    private readonly ConfigService _config;
    private readonly Dictionary<WidgetKind, IDockWidget> _widgets;
    private readonly INotificationWidget? _notifications;
    private bool _notificationsActive;
    private readonly bool _weatherInstalled;

    public event Action<WidgetNotification>? NotificationRaised;
    public event Action? NotificationsCleared;

    public WidgetHost(StatusControl control, ConfigService config)
    {
        _control = control;
        _config = config;
        var weather = LoadWeather(config);
        _weatherInstalled = weather != null;
        _widgets = new()
        {
            [WidgetKind.Weather] = weather ?? new MissingWeatherWidget(),
            [WidgetKind.Device] = new DeviceWidget(config),
            [WidgetKind.Music] = new MusicWidget(),
            [WidgetKind.Quota] = new QuotaWidget()
        };

        _notifications = weather as INotificationWidget;
        if (_notifications != null)
        {
            _notifications.NotificationRaised += OnNotificationRaised;
            _notifications.NotificationsCleared += OnNotificationsCleared;
        }
    }

    private static IDockWidget? LoadWeather(ConfigService config)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Widgets", "QuickDock.Weather.dll");
        if (!File.Exists(path)) return null;

        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
            var type = assembly.GetType("QuickDock.Weather.WeatherWidget", throwOnError: true)!;
            var context = new WidgetContext(
                Path.GetDirectoryName(config.GetConfigPath())!,
                config.Settings.LegacySettings,
                () => config.Settings.Language == "zh");
            return (IDockWidget?)Activator.CreateInstance(type, context);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Weather widget could not load: {ex}");
            return null;
        }
    }

    public string GetLabel(WidgetKind kind) => kind switch
    {
        WidgetKind.Weather => Lang.T(_weatherInstalled ? "Widget.Weather" : "Widget.WeatherMissing"),
        WidgetKind.Device => Lang.T("Widget.Device"),
        WidgetKind.Music => Lang.T("Widget.Music"),
        WidgetKind.Quota => Lang.T("Widget.Quota"),
        _ => kind.ToString()
    };

    public bool CanConfigure(WidgetKind kind) => _widgets[kind] is IConfigurableWidget;

    public void Configure(WidgetKind kind, Window owner)
    {
        if (_widgets[kind] is IConfigurableWidget widget) widget.ShowSettings(owner);
    }

    public void ApplySlots()
    {
        var slots = _config.Settings.Widgets;
        var show = _config.Settings.ShowStatusBar;
        var notificationsActive = show && (slots.First == WidgetKind.Weather || slots.Second == WidgetKind.Weather);
        if (_notificationsActive && !notificationsActive) OnNotificationsCleared();
        _notificationsActive = notificationsActive;

        _control.ShowWidgets(_widgets[slots.First].View, _widgets[slots.Second].View);
        foreach (var (kind, widget) in _widgets)
        {
            widget.SetActive(show && (kind == slots.First || kind == slots.Second));
        }
    }

    public void UpdateLanguage()
    {
        foreach (var widget in _widgets.Values) widget.UpdateLanguage();
    }

    private void OnNotificationRaised(WidgetNotification notification)
    {
        if (_notificationsActive) NotificationRaised?.Invoke(notification);
    }

    private void OnNotificationsCleared() => NotificationsCleared?.Invoke();

    public WidgetNotification? GetLatestNotification() =>
        _notificationsActive ? _notifications?.GetLatestNotification() : null;

    public void MuteUntilRefresh(string group) => _notifications?.MuteUntilRefresh(group);

    public void Dispose()
    {
        if (_notifications != null)
        {
            _notifications.NotificationRaised -= OnNotificationRaised;
            _notifications.NotificationsCleared -= OnNotificationsCleared;
        }
        foreach (var widget in _widgets.Values) widget.Dispose();
    }

    private sealed class MissingWeatherWidget : IDockWidget
    {
        private readonly System.Windows.Controls.TextBlock _view = WidgetView.Text();
        public MissingWeatherWidget() => UpdateLanguage();
        public FrameworkElement View => _view;
        public void SetActive(bool active) { }
        public void UpdateLanguage() => _view.Text = Lang.T("Widget.WeatherMissing");
        public void Dispose() { }
    }
}
