using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Threading;
using QuickDock.Models;

namespace QuickDock.Services;

public class StatusService : INotifyPropertyChanged, IDisposable
{
    private readonly ConfigService _configService;
    private readonly WeatherService _weatherService;
    private readonly SystemResourceService _resourceService;
    private readonly DispatcherTimer _weatherTimer;
    private readonly DispatcherTimer _resourceTimer;
    private readonly System.Windows.Threading.Dispatcher _dispatcher;
    
    private WeatherData? _weatherData;
    private string _weatherIcon = "☀";
    private string _weatherText = "加载中...";
    private int _cpuPercent;
    private int _memPercent;
    private int _gpuPercent;
    private bool _isLoading;
    private string? _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RefreshRequested;

    public WeatherData? WeatherData
    {
        get => _weatherData;
        private set { _weatherData = value; OnPropertyChanged(); }
    }

    public string WeatherIcon
    {
        get => _weatherIcon;
        private set { _weatherIcon = value; OnPropertyChanged(); }
    }

    public string WeatherText
    {
        get => _weatherText;
        private set { _weatherText = value; OnPropertyChanged(); }
    }

    public int CpuPercent
    {
        get => _cpuPercent;
        private set { _cpuPercent = value; OnPropertyChanged(); }
    }

    public int MemPercent
    {
        get => _memPercent;
        private set { _memPercent = value; OnPropertyChanged(); }
    }

    public int GpuPercent
    {
        get => _gpuPercent;
        private set { _gpuPercent = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set { _errorMessage = value; OnPropertyChanged(); }
    }

    public StatusService(ConfigService configService)
    {
        _configService = configService;
        _dispatcher = System.Windows.Threading.Dispatcher.CurrentDispatcher;
        _weatherService = new WeatherService(configService);
        _resourceService = new SystemResourceService();

        _weatherTimer = new DispatcherTimer();
        _weatherTimer.Tick += async (s, e) => await RefreshWeatherAsync();

        _resourceTimer = new DispatcherTimer();
        _resourceTimer.Tick += (s, e) => RefreshResources();
        _configService.SettingsChanged += OnSettingsChanged;
        ApplyTimerIntervals();

        InitializeAsync();
    }

    private void OnSettingsChanged(string? propertyName)
    {
        switch (propertyName)
        {
            case nameof(AppSettings.Language):
                UpdateWeatherUI("☀", _weatherData == null ? (Lang.CurrentLanguage == Language.Chinese ? "加载中..." : "Loading...") : WeatherText);
                break;
            case nameof(AppSettings.WeatherCity):
                _weatherService.ClearCache();
                _ = RefreshWeatherAsync();
                break;
            case nameof(AppSettings.WeatherRefreshIntervalMinutes):
            case nameof(AppSettings.ResourceRefreshIntervalSeconds):
                ApplyTimerIntervals();
                break;
        }
    }

    private void ApplyTimerIntervals()
    {
        _weatherTimer.Interval = TimeSpan.FromMinutes(Math.Max(1, _configService.Settings.WeatherRefreshIntervalMinutes));
        _resourceTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, _configService.Settings.ResourceRefreshIntervalSeconds));
    }

    private async void InitializeAsync()
    {
        IsLoading = true;
        
        RefreshResources();
        
        try
        {
            await RefreshWeatherAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Weather init error: {ex}");
            UpdateWeatherUI("☀", "获取失败");
        }
        
        _weatherTimer.Start();
        _resourceTimer.Start();
        
        IsLoading = false;
    }

    private void UpdateWeatherUI(string icon, string text)
    {
        _dispatcher.Invoke(() =>
        {
            WeatherIcon = icon;
            WeatherText = text;
        });
    }

    public async Task RefreshAllAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        
        try
        {
            RefreshResources();
            await RefreshWeatherAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"刷新失败: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RefreshWeatherAsync()
    {
        try
        {
            var data = await _weatherService.GetWeatherAsync();
            if (data != null)
            {
                WeatherData = data;
                var icon = WeatherService.GetWeatherIcon(data.Weather?.ConditionCode ?? string.Empty);
                var text = $"{data.Weather?.Temperature ?? 0}°C {data.Weather?.Condition ?? ""}";
                
                _dispatcher.Invoke(() =>
                {
                    WeatherIcon = icon;
                    WeatherText = text;
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Weather refresh error: {ex}");
            UpdateWeatherUI("☀", "获取失败");
        }
    }

    private void RefreshResources()
    {
        try
        {
            var info = _resourceService.GetResourceInfo();
            _dispatcher.Invoke(() =>
            {
                CpuPercent = info.CpuPercent;
                MemPercent = info.MemPercent;
                GpuPercent = info.GpuPercent;
            });
        }
        catch
        {
        }
    }

    public void RequestRefresh()
    {
        RefreshRequested?.Invoke();
        _ = RefreshAllAsync();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void Dispose()
    {
        _configService.SettingsChanged -= OnSettingsChanged;
        _weatherTimer?.Stop();
        _resourceTimer?.Stop();
    }
}
