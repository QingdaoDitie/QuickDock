using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuickDock.Models;
using QuickDock.Services;

namespace QuickDock.Controls;

public partial class DockItemControl : System.Windows.Controls.UserControl
{
    private static readonly LaunchService _launchService = new();
    public static ConfigService? SharedConfigService;

    public DockItemControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var configService = SharedConfigService;
        if (configService != null)
        {
            var iconSize = configService.Settings.IconSize;
            IconRect.Width = iconSize;
            IconRect.Height = iconSize;
        }
        
        if (DataContext is DockItem item)
        {
            LoadIcon(item);
        }
        else if (DataContext is ToolItem tool)
        {
            LoadToolIcon(tool);
        }
    }

    private void LoadToolIcon(ToolItem tool)
    {
        try
        {
            if (!string.IsNullOrEmpty(tool.CustomIconPath))
            {
                if (File.Exists(tool.CustomIconPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(tool.CustomIconPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    ItemIconBrush.ImageSource = bitmap;
                    FallbackText.Visibility = Visibility.Collapsed;
                    IconBorder.Visibility = Visibility.Visible;
                    return;
                }
            }

            if (File.Exists(tool.ExePath))
            {
                var icon = System.Drawing.Icon.ExtractAssociatedIcon(tool.ExePath);
                if (icon != null)
                {
                    ConvertIconToBitmapSource(icon);
                    return;
                }
            }
        }
        catch { }

        var defaultIcon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "tools.png");
        if (File.Exists(defaultIcon))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(defaultIcon, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ItemIconBrush.ImageSource = bitmap;
                FallbackText.Visibility = Visibility.Collapsed;
                IconBorder.Visibility = Visibility.Visible;
                return;
            }
            catch { }
        }

        ShowToolFallback(tool);
    }

    private void ShowToolFallback(ToolItem tool)
    {
        FallbackText.Text = tool.DisplayName.Length > 2 
            ? tool.DisplayName.Substring(0, 2).ToUpper() 
            : tool.DisplayName.ToUpper();
        FallbackText.Visibility = Visibility.Visible;
        IconBorder.Visibility = Visibility.Collapsed;
    }

    private void LoadIcon(DockItem item)
    {
        try
        {
            if (!string.IsNullOrEmpty(item.IconPath))
            {
                var absolutePath = ToAbsolutePath(item.IconPath);
                if (LoadIconFromFile(absolutePath))
                    return;
            }

            if (!string.IsNullOrEmpty(item.Path))
            {
                if (LoadIconFromPath(item))
                    return;
            }

            if (LoadSystemIconForType(item))
                return;
        }
        catch { }

        ShowFallback(item);
    }

    private string ToAbsolutePath(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (Path.IsPathRooted(path)) return path;
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
    }

    private bool LoadIconFromFile(string iconPath)
    {
        try
        {
            if (File.Exists(iconPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                ItemIconBrush.ImageSource = bitmap;
                FallbackText.Visibility = Visibility.Collapsed;
                IconBorder.Visibility = Visibility.Visible;
                return true;
            }
        }
        catch { }
        return false;
    }

    private string? ResolveFullPath(string path)
    {
        if (File.Exists(path))
            return path;

        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var fullPath = Path.Combine(system32, path);
        if (File.Exists(fullPath))
            return fullPath;

        var windowsDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var sysWow64 = Path.Combine(windowsDir, "SysWOW64");
        fullPath = Path.Combine(sysWow64, path);
        if (File.Exists(fullPath))
            return fullPath;

        var powerShellPath = Path.Combine(system32, "WindowsPowerShell", "v1.0", path);
        if (File.Exists(powerShellPath))
            return powerShellPath;

        powerShellPath = Path.Combine(sysWow64, "WindowsPowerShell", "v1.0", path);
        if (File.Exists(powerShellPath))
            return powerShellPath;

        var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach (var p in paths)
        {
            try
            {
                fullPath = Path.Combine(p.Trim(), path);
                if (File.Exists(fullPath))
                    return fullPath;
            }
            catch { }
        }

        return null;
    }

    private bool LoadIconFromPath(DockItem item)
    {
        try
        {
            System.Drawing.Icon? icon = null;

            if (item.Type == DockItemType.Application)
            {
                var fullPath = ResolveFullPath(item.Path);
                if (fullPath != null && File.Exists(fullPath))
                {
                    icon = System.Drawing.Icon.ExtractAssociatedIcon(fullPath);
                }
            }
            else if (item.Type == DockItemType.Folder && Directory.Exists(item.Path))
            {
                icon = _launchService.GetSystemIcon(DockItemType.Folder);
            }
            else if (item.Type == DockItemType.WebPage)
            {
                icon = _launchService.GetSystemIcon(DockItemType.WebPage);
            }
            else if (item.Type == DockItemType.Command)
            {
                icon = _launchService.GetSystemIcon(DockItemType.Command);
            }

            if (icon != null)
            {
                return ConvertIconToBitmapSource(icon);
            }
        }
        catch { }
        return false;
    }

    private bool LoadSystemIconForType(DockItem item)
    {
        try
        {
            var icon = _launchService.GetSystemIcon(item.Type);
            if (icon != null)
            {
                return ConvertIconToBitmapSource(icon);
            }
        }
        catch { }
        return false;
    }

    private bool ConvertIconToBitmapSource(System.Drawing.Icon icon)
    {
        try
        {
            var bitmap = icon.ToBitmap();
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                ItemIconBrush.ImageSource = bitmapSource;
                FallbackText.Visibility = Visibility.Collapsed;
                IconBorder.Visibility = Visibility.Visible;
                return true;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
        catch { }
        return false;
    }

    private void ShowFallback(DockItem item)
    {
        FallbackText.Text = item.Name.Length > 2 ? item.Name.Substring(0, 2).ToUpper() : item.Name.ToUpper();
        FallbackText.Visibility = Visibility.Visible;
        IconBorder.Visibility = Visibility.Collapsed;
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is DockItem item)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.LaunchItem(item);
            }
        }
        else if (DataContext is ToolItem tool)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.LaunchToolItem(tool);
            }
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var configService = SharedConfigService;
        if (configService != null)
        {
            var iconSize = configService.Settings.IconSize;
            var padding = 18.0;
            Width = iconSize + padding;
            Height = iconSize + padding;
            IconRect.Width = iconSize;
            IconRect.Height = iconSize;
        }
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
