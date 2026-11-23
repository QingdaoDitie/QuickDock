using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using QuickDock.Models;
using QuickDock.Services;
using QuickDock.WidgetContracts;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace QuickDock.Widgets;

internal sealed class DeviceWidget : IDockWidget
{
    private readonly ConfigService _config;
    private readonly DispatcherTimer _timer = new();
    private readonly TextBlock[] _values;
    private SystemResourceService? _resources;
    public FrameworkElement View { get; }

    public DeviceWidget(ConfigService config)
    {
        _config = config;
        var row = new StackPanel { Orientation = WpfOrientation.Horizontal };
        _values = new[] { "CPU", "MEM", "GPU" }.Select(label =>
        {
            var column = new StackPanel { Width = 32 };
            var caption = WidgetView.Text(10, subdued: true);
            caption.Text = label;
            caption.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            var value = WidgetView.Text(11);
            value.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            value.FontFamily = new System.Windows.Media.FontFamily("Consolas");
            column.Children.Add(caption);
            column.Children.Add(value);
            row.Children.Add(column);
            return value;
        }).ToArray();
        View = row;
        _timer.Tick += (_, _) => Refresh();
        ApplyInterval(null);
        _config.SettingsChanged += ApplyInterval;
    }

    private void ApplyInterval(string? property)
    {
        if (property is null or nameof(AppSettings.ResourceRefreshIntervalSeconds))
            _timer.Interval = TimeSpan.FromSeconds(Math.Max(1, _config.Settings.ResourceRefreshIntervalSeconds));
    }

    public void SetActive(bool active)
    {
        if (!active)
        {
            _timer.Stop();
            _resources?.Dispose();
            _resources = null;
            return;
        }

        if (_timer.IsEnabled) return;
        _resources = new SystemResourceService();
        Refresh();
        _timer.Start();
    }

    private void Refresh()
    {
        if (_resources == null) return;
        var info = _resources.GetResourceInfo();
        _values[0].Text = $"{info.CpuPercent}%";
        _values[1].Text = $"{info.MemPercent}%";
        _values[2].Text = $"{info.GpuPercent}%";
    }

    public void UpdateLanguage() { }

    public void Dispose()
    {
        _config.SettingsChanged -= ApplyInterval;
        SetActive(false);
    }
}
