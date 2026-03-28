namespace QuickDock.Models;

public class AppSettings
{
    public bool AutoStart { get; set; } = true;
    public double HotZoneWidth { get; set; } = 0.3;
    public int AnimationDuration { get; set; } = 200;
    public string Language { get; set; } = "zh";
    public double DockOpacity { get; set; } = 0.9;
    
    public string BackgroundColor { get; set; } = "#1e1e1e";
    public double Scale { get; set; } = 1.0;
    public double IconSize { get; set; } = 32;
    public double IconSpacing { get; set; } = 5;
    
    public bool ShowStatusBar { get; set; } = true;
    public string WeatherCity { get; set; } = "";
}
