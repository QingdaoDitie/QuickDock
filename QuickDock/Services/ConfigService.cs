using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.ComponentModel;
using QuickDock.Models;

namespace QuickDock.Services;

public class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    private static readonly string IconsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons");
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public event Action<string?>? SettingsChanged;

    public List<DockItem> Items { get; private set; } = new();
    public AppSettings Settings { get; private set; } = new();

    public ConfigService()
    {
        EnsureIconsDirectory();
        Load();
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Lang.CurrentLanguage = Settings.Language == "zh" ? Language.Chinese : Language.English;
    }

    private void SubscribeToSettings(AppSettings settings)
    {
        settings.PropertyChanged -= OnSettingsPropertyChanged;
        settings.PropertyChanged += OnSettingsPropertyChanged;
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        ApplyLanguage();
        SettingsChanged?.Invoke(e.PropertyName);
    }

    private void MigrateSettings(AppSettings settings)
    {
        var changed = false;

        if (settings.HotZoneTriggerDelay <= 0)
        {
            settings.HotZoneTriggerDelay = 500;
            changed = true;
        }

        if (settings.HotZoneEdgeSize <= 0)
        {
            settings.HotZoneEdgeSize = 1;
            changed = true;
        }

        if (settings.ToolsItems == null)
        {
            settings.ToolsItems = new List<ToolItem>();
            changed = true;
        }

        if (settings.ToolIconSize <= 0)
        {
            settings.ToolIconSize = 24;
            changed = true;
        }

        if (settings.ToolsAnimationDuration <= 0)
        {
            settings.ToolsAnimationDuration = 120;
            changed = true;
        }

        if (settings.DockShowAnimationDuration <= 0)
        {
            settings.DockShowAnimationDuration = settings.AnimationDuration > 0 ? settings.AnimationDuration : 220;
            changed = true;
        }

        if (settings.DockHideAnimationDuration <= 0)
        {
            settings.DockHideAnimationDuration = settings.AnimationDuration > 0 ? settings.AnimationDuration : 180;
            changed = true;
        }

        if (settings.ToolsExpandAnimationDuration <= 0)
        {
            settings.ToolsExpandAnimationDuration = settings.ToolsAnimationDuration > 0 ? settings.ToolsAnimationDuration : 140;
            changed = true;
        }

        if (settings.ToolsCollapseAnimationDuration <= 0)
        {
            settings.ToolsCollapseAnimationDuration = settings.ToolsAnimationDuration > 0 ? settings.ToolsAnimationDuration : 110;
            changed = true;
        }

        if (settings.StartupPreviewDuration < 0)
        {
            settings.StartupPreviewDuration = 1500;
            changed = true;
        }

        if (settings.AutoHideTolerance < 0)
        {
            settings.AutoHideTolerance = 10;
            changed = true;
        }

        if (settings.WeatherRefreshIntervalMinutes <= 0)
        {
            settings.WeatherRefreshIntervalMinutes = 30;
            changed = true;
        }

        if (settings.ResourceRefreshIntervalSeconds <= 0)
        {
            settings.ResourceRefreshIntervalSeconds = 5;
            changed = true;
        }

        if (string.IsNullOrWhiteSpace(settings.SettingsBackgroundColor))
        {
            settings.SettingsBackgroundColor = "#f5f6f8";
            changed = true;
        }

        if (changed)
        {
            Save();
        }
    }

    private void EnsureIconsDirectory()
    {
        if (!Directory.Exists(IconsPath))
        {
            Directory.CreateDirectory(IconsPath);
        }
    }

    public void Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var json = File.ReadAllText(ConfigPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);
                if (config != null)
                {
                    Items = config.Items ?? new List<DockItem>();
                    Settings = config.Settings ?? new AppSettings();
                    SubscribeToSettings(Settings);
                    MigrateSettings(Settings);
                    ApplyLanguage();
                }
            }
            catch
            {
                LoadDefault();
            }
        }
        else
        {
            LoadDefault();
        }
    }

    private void LoadDefault()
    {
        Items = new List<DockItem>
        {
            new DockItem
            {
                Id = "1",
                Name = "CMD",
                Type = DockItemType.Application,
                Path = "cmd.exe",
                RunAsAdmin = false
            },
            new DockItem
            {
                Id = "2",
                Name = "PowerShell (Admin)",
                Type = DockItemType.Application,
                Path = "powershell.exe",
                RunAsAdmin = true
            }
        };
        Settings = new AppSettings();
        SubscribeToSettings(Settings);
        Save();
    }

    public void Save()
    {
        foreach (var item in Items)
        {
            item.IconPath = ToRelativePath(item.IconPath);
        }
        
        var config = new AppConfig
        {
            Items = Items,
            Settings = Settings
        };
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
    
    public string ToRelativePath(string? absolutePath)
    {
        if (string.IsNullOrEmpty(absolutePath)) return "";
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        if (absolutePath.StartsWith(baseDir, StringComparison.OrdinalIgnoreCase))
        {
            return absolutePath.Substring(baseDir.Length);
        }
        return absolutePath;
    }
    
    public string ToAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return "";
        if (Path.IsPathRooted(relativePath)) return relativePath;
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);
    }

    public void AddItem(DockItem item)
    {
        Items.Add(item);
        Save();
    }

    public void RemoveItem(string id)
    {
        Items.RemoveAll(i => i.Id == id);
        Save();
    }

    public void UpdateItem(DockItem item)
    {
        var index = Items.FindIndex(i => i.Id == item.Id);
        if (index >= 0)
        {
            Items[index] = item;
            Save();
        }
    }

    public void MoveItem(int oldIndex, int newIndex)
    {
        if (oldIndex >= 0 && oldIndex < Items.Count && newIndex >= 0 && newIndex < Items.Count)
        {
            var item = Items[oldIndex];
            Items.RemoveAt(oldIndex);
            Items.Insert(newIndex, item);
            Save();
        }
    }

    public string GetIconsPath() => IconsPath;
    public string GetConfigPath() => ConfigPath;
}

public class AppConfig
{
    public List<DockItem>? Items { get; set; }
    public AppSettings? Settings { get; set; }
}
