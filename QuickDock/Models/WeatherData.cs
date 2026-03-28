using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuickDock.Models;

public class WeatherResponse
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("message")]
    public string? Message { get; set; }
    
    [JsonPropertyName("data")]
    public WeatherData? Data { get; set; }
}

public class WeatherData
{
    [JsonPropertyName("location")]
    public WeatherLocation? Location { get; set; }
    
    [JsonPropertyName("weather")]
    public WeatherInfo? Weather { get; set; }
    
    [JsonPropertyName("air_quality")]
    public AirQuality? AirQuality { get; set; }
    
    [JsonPropertyName("alerts")]
    public List<WeatherAlert>? Alerts { get; set; }
}

public class WeatherLocation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("province")]
    public string? Province { get; set; }
    
    [JsonPropertyName("city")]
    public string? City { get; set; }
    
    [JsonPropertyName("county")]
    public string? County { get; set; }
}

public class WeatherInfo
{
    [JsonPropertyName("condition")]
    public string? Condition { get; set; }
    
    [JsonPropertyName("condition_code")]
    public string? ConditionCode { get; set; }
    
    [JsonPropertyName("temperature")]
    public int Temperature { get; set; }
    
    [JsonPropertyName("humidity")]
    public int Humidity { get; set; }
    
    [JsonPropertyName("wind_direction")]
    public string? WindDirection { get; set; }
    
    [JsonPropertyName("wind_power")]
    public string? WindPower { get; set; }
    
    [JsonPropertyName("updated")]
    public string? Updated { get; set; }
}

public class AirQuality
{
    [JsonPropertyName("aqi")]
    public int Aqi { get; set; }
    
    [JsonPropertyName("quality")]
    public string? Quality { get; set; }
}

public class WeatherAlert
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("level")]
    public string? Level { get; set; }
    
    [JsonPropertyName("detail")]
    public string? Detail { get; set; }
}
