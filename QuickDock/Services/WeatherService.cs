using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using QuickDock.Models;

namespace QuickDock.Services;

public class WeatherService
{
    private readonly HttpClient _httpClient;
    private readonly ConfigService _configService;
    private WeatherData? _cachedWeather;
    private DateTime _lastUpdate;
    private string? _currentCity;
    
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);
    
    public event Action<WeatherData>? WeatherUpdated;
    public event Action<string>? ErrorOccurred;
    
    public WeatherData? CurrentWeather => _cachedWeather;
    public string? CurrentCity => _currentCity;
    public bool HasValidCache => _cachedWeather != null && DateTime.Now - _lastUpdate < CacheDuration;

    public WeatherService(ConfigService configService)
    {
        _configService = configService;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public async Task<WeatherData?> GetWeatherAsync(string? city = null)
    {
        if (HasValidCache && string.IsNullOrEmpty(city))
        {
            return _cachedWeather;
        }

        try
        {
            if (string.IsNullOrEmpty(city))
            {
                city = _configService.Settings.WeatherCity;
                
                if (string.IsNullOrEmpty(city))
                {
                    ErrorOccurred?.Invoke("未配置城市");
                    return null;
                }
            }

            _currentCity = city;
            
            var url = $"https://60s.viki.moe/v2/weather?query={city}";
            System.Diagnostics.Debug.WriteLine($"Requesting weather: {url}");
            
            var response = await _httpClient.GetStringAsync(url);
            System.Diagnostics.Debug.WriteLine($"Weather response: {response}");
            
            var weatherResponse = JsonSerializer.Deserialize<WeatherResponse>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            System.Diagnostics.Debug.WriteLine($"Parsed: Code={weatherResponse?.Code}, Data={weatherResponse?.Data != null}");

            if (weatherResponse?.Data != null)
            {
                _cachedWeather = weatherResponse.Data;
                _lastUpdate = DateTime.Now;
                WeatherUpdated?.Invoke(_cachedWeather);
                return _cachedWeather;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Weather error: {ex}");
            ErrorOccurred?.Invoke($"获取天气失败: {ex.Message}");
            return _cachedWeather;
        }
    }

    public static string GetWeatherIcon(string conditionCode)
    {
        if (string.IsNullOrEmpty(conditionCode))
            return "☀";
        
        return conditionCode switch
        {
            "00" => "☀",
            "01" => "🌤",
            "02" => "☁",
            "03" => "☁",
            "04" => "☁",
            "05" => "🌫",
            "06" => "🌧",
            "07" => "⛈",
            "08" => "🌨",
            "09" => "🌨",
            "10" => "🌨",
            "11" => "🌨",
            "12" => "🌧",
            "13" => "🌧",
            "14" => "🌧",
            "15" => "🌨",
            "16" => "🌨",
            "17" => "🌨",
            "18" => "🌫",
            "19" => "🌫",
            "20" => "🌪",
            "21" => "🌧",
            "22" => "🌧",
            "23" => "🌨",
            "24" => "🌨",
            "25" => "🌨",
            "26" => "🌧",
            "27" => "🌧",
            "28" => "🌧",
            "29" => "🌫",
            "30" => "🌫",
            "31" => "🌪",
            "53" => "🌫",
            "99" => "☀",
            _ => "☀"
        };
    }

    public void ClearCache()
    {
        _cachedWeather = null;
        _currentCity = null;
        _lastUpdate = DateTime.MinValue;
    }
}
