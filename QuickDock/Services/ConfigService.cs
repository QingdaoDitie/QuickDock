using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                    MigrateSettings(Settings);
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
}

public class AppConfig
{
    public List<DockItem>? Items { get; set; }
    public AppSettings? Settings { get; set; }
}
