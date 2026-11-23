using System.Text.Json;
using System.Windows;

namespace QuickDock.WidgetContracts;

public interface IDockWidget : IDisposable
{
    FrameworkElement View { get; }
    void SetActive(bool active);
    void UpdateLanguage();
}

public interface IConfigurableWidget
{
    void ShowSettings(Window owner);
}

public interface INotificationWidget
{
    event Action<WidgetNotification>? NotificationRaised;
    event Action? NotificationsCleared;
    WidgetNotification? GetLatestNotification();
    void MuteUntilRefresh(string group);
}

public sealed record WidgetContext(
    string SettingsDirectory,
    IReadOnlyDictionary<string, JsonElement> LegacySettings,
    Func<bool> IsChinese);

public sealed record WidgetNotification(
    string Key,
    string Group,
    string Icon,
    string Title,
    string Message,
    DateTimeOffset PublishedAt,
    int Priority,
    Func<FrameworkElement>? CreateDetails = null,
    int ShowMilliseconds = 220,
    int DisplayMilliseconds = 2500,
    int HideMilliseconds = 180);
