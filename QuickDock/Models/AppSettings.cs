using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace QuickDock.Models;

public enum PopupMode
{
    Normal,
    Game,
    DoNotDisturb
}

public class AppSettings : INotifyPropertyChanged
{
    private bool _autoStart = false;
    private PopupMode _popupMode;
    private double _hotZoneWidth = 0.3;
    private int _hotZoneTriggerDelay = 500;
    private int _hotZoneEdgeSize = 1;
    private int _animationDuration = 200;
    private int _toolsAnimationDuration = 120;
    private int _dockShowAnimationDuration = 220;
    private int _dockHideAnimationDuration = 180;
    private int _toolsExpandAnimationDuration = 140;
    private int _toolsCollapseAnimationDuration = 110;
    private int _startupPreviewDuration = 1500;
    private int _autoHideTolerance = 10;
    private int _resourceRefreshIntervalSeconds = 5;
    private string _language = "zh";
    private double _dockOpacity = 0.9;
    private string _backgroundColor = "#1e1e1e";
    private string _settingsBackgroundColor = "#f5f6f8";
    private double _scale = 1.0;
    private double _iconSize = 32;
    private double _iconSpacing = 5;
    private double _toolIconSize = 24;
    private bool _showStatusBar = true;
    private bool _toolsEnabled;
    private string _toolsRootPath = "";
    private List<ToolItem> _toolsItems = new();

    private WidgetSlots _widgets = WidgetSlots.Default;

    public WidgetSlots Widgets
    {
        get => _widgets;
        set => SetProperty(ref _widgets, value is { IsValid: true } ? value : WidgetSlots.Default);
    }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> LegacySettings { get; set; } = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool AutoStart
    {
        get => _autoStart;
        set => SetProperty(ref _autoStart, value);
    }

    public double HotZoneWidth
    {
        get => _hotZoneWidth;
        set => SetProperty(ref _hotZoneWidth, value);
    }

    public PopupMode PopupMode
    {
        get => _popupMode;
        set => SetProperty(ref _popupMode, value);
    }

    public int HotZoneTriggerDelay
    {
        get => _hotZoneTriggerDelay;
        set => SetProperty(ref _hotZoneTriggerDelay, value);
    }

    public int HotZoneEdgeSize
    {
        get => _hotZoneEdgeSize;
        set => SetProperty(ref _hotZoneEdgeSize, value);
    }

    public int AnimationDuration
    {
        get => _animationDuration;
        set => SetProperty(ref _animationDuration, value);
    }

    public int ToolsAnimationDuration
    {
        get => _toolsAnimationDuration;
        set => SetProperty(ref _toolsAnimationDuration, value);
    }

    public int DockShowAnimationDuration
    {
        get => _dockShowAnimationDuration;
        set => SetProperty(ref _dockShowAnimationDuration, value);
    }

    public int DockHideAnimationDuration
    {
        get => _dockHideAnimationDuration;
        set => SetProperty(ref _dockHideAnimationDuration, value);
    }

    public int ToolsExpandAnimationDuration
    {
        get => _toolsExpandAnimationDuration;
        set => SetProperty(ref _toolsExpandAnimationDuration, value);
    }

    public int ToolsCollapseAnimationDuration
    {
        get => _toolsCollapseAnimationDuration;
        set => SetProperty(ref _toolsCollapseAnimationDuration, value);
    }

    public int StartupPreviewDuration
    {
        get => _startupPreviewDuration;
        set => SetProperty(ref _startupPreviewDuration, value);
    }

    public int AutoHideTolerance
    {
        get => _autoHideTolerance;
        set => SetProperty(ref _autoHideTolerance, value);
    }

    public int ResourceRefreshIntervalSeconds
    {
        get => _resourceRefreshIntervalSeconds;
        set => SetProperty(ref _resourceRefreshIntervalSeconds, value);
    }

    public string Language
    {
        get => _language;
        set => SetProperty(ref _language, value);
    }

    public double DockOpacity
    {
        get => _dockOpacity;
        set => SetProperty(ref _dockOpacity, value);
    }

    public string BackgroundColor
    {
        get => _backgroundColor;
        set => SetProperty(ref _backgroundColor, value);
    }

    public string SettingsBackgroundColor
    {
        get => _settingsBackgroundColor;
        set => SetProperty(ref _settingsBackgroundColor, value);
    }

    public double Scale
    {
        get => _scale;
        set => SetProperty(ref _scale, value);
    }

    public double IconSize
    {
        get => _iconSize;
        set => SetProperty(ref _iconSize, value);
    }

    public double IconSpacing
    {
        get => _iconSpacing;
        set => SetProperty(ref _iconSpacing, value);
    }

    public double ToolIconSize
    {
        get => _toolIconSize;
        set => SetProperty(ref _toolIconSize, value);
    }

    public bool ShowStatusBar
    {
        get => _showStatusBar;
        set => SetProperty(ref _showStatusBar, value);
    }

    public bool ToolsEnabled
    {
        get => _toolsEnabled;
        set => SetProperty(ref _toolsEnabled, value);
    }

    public string ToolsRootPath
    {
        get => _toolsRootPath;
        set => SetProperty(ref _toolsRootPath, value);
    }

    public List<ToolItem> ToolsItems
    {
        get => _toolsItems;
        set => SetProperty(ref _toolsItems, value ?? new List<ToolItem>());
    }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            AutoStart = AutoStart,
            PopupMode = PopupMode,
            HotZoneWidth = HotZoneWidth,
            HotZoneTriggerDelay = HotZoneTriggerDelay,
            HotZoneEdgeSize = HotZoneEdgeSize,
            AnimationDuration = AnimationDuration,
            ToolsAnimationDuration = ToolsAnimationDuration,
            DockShowAnimationDuration = DockShowAnimationDuration,
            DockHideAnimationDuration = DockHideAnimationDuration,
            ToolsExpandAnimationDuration = ToolsExpandAnimationDuration,
            ToolsCollapseAnimationDuration = ToolsCollapseAnimationDuration,
            StartupPreviewDuration = StartupPreviewDuration,
            AutoHideTolerance = AutoHideTolerance,
            ResourceRefreshIntervalSeconds = ResourceRefreshIntervalSeconds,
            Language = Language,
            DockOpacity = DockOpacity,
            BackgroundColor = BackgroundColor,
            SettingsBackgroundColor = SettingsBackgroundColor,
            Scale = Scale,
            IconSize = IconSize,
            IconSpacing = IconSpacing,
            ToolIconSize = ToolIconSize,
            ShowStatusBar = ShowStatusBar,
            Widgets = Widgets,
            ToolsEnabled = ToolsEnabled,
            ToolsRootPath = ToolsRootPath,
            ToolsItems = ToolsItems.Select(CloneToolItem).ToList()
        };
    }

    public void CopyFrom(AppSettings source)
    {
        AutoStart = source.AutoStart;
        PopupMode = source.PopupMode;
        HotZoneWidth = source.HotZoneWidth;
        HotZoneTriggerDelay = source.HotZoneTriggerDelay;
        HotZoneEdgeSize = source.HotZoneEdgeSize;
        AnimationDuration = source.AnimationDuration;
        ToolsAnimationDuration = source.ToolsAnimationDuration;
        DockShowAnimationDuration = source.DockShowAnimationDuration;
        DockHideAnimationDuration = source.DockHideAnimationDuration;
        ToolsExpandAnimationDuration = source.ToolsExpandAnimationDuration;
        ToolsCollapseAnimationDuration = source.ToolsCollapseAnimationDuration;
        StartupPreviewDuration = source.StartupPreviewDuration;
        AutoHideTolerance = source.AutoHideTolerance;
        ResourceRefreshIntervalSeconds = source.ResourceRefreshIntervalSeconds;
        Language = source.Language;
        DockOpacity = source.DockOpacity;
        BackgroundColor = source.BackgroundColor;
        SettingsBackgroundColor = source.SettingsBackgroundColor;
        Scale = source.Scale;
        IconSize = source.IconSize;
        IconSpacing = source.IconSpacing;
        ToolIconSize = source.ToolIconSize;
        ShowStatusBar = source.ShowStatusBar;
        Widgets = source.Widgets;
        ToolsEnabled = source.ToolsEnabled;
        ToolsRootPath = source.ToolsRootPath;
        ToolsItems = source.ToolsItems.Select(CloneToolItem).ToList();
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private static ToolItem CloneToolItem(ToolItem tool)
    {
        return new ToolItem
        {
            Id = tool.Id,
            DisplayName = tool.DisplayName,
            ExePath = tool.ExePath,
            SourceFolder = tool.SourceFolder,
            IsConfirmed = tool.IsConfirmed,
            CustomIconPath = tool.CustomIconPath,
            Order = tool.Order,
            Enabled = tool.Enabled
        };
    }
}
