using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuickDock.Models;

namespace QuickDock.Services;

public class ItemIconConverter : IValueConverter
{
    private readonly LaunchService _launchService = new();

    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            DockItem item => LoadDockItemIcon(item),
            ToolItem tool => LoadToolItemIcon(tool),
            _ => null
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

    private ImageSource? LoadDockItemIcon(DockItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.IconPath))
        {
            var absolutePath = ToAbsolutePath(item.IconPath);
            var localImage = LoadBitmapFromFile(absolutePath);
            if (localImage != null)
            {
                return localImage;
            }
        }

        if (!string.IsNullOrWhiteSpace(item.Path))
        {
            var fromPath = LoadDockIconFromPath(item);
            if (fromPath != null)
            {
                return fromPath;
            }
        }

        return ConvertIconToImageSource(_launchService.GetSystemIcon(item.Type));
    }

    private ImageSource? LoadToolItemIcon(ToolItem tool)
    {
        if (!string.IsNullOrWhiteSpace(tool.CustomIconPath))
        {
            var localImage = LoadBitmapFromFile(tool.CustomIconPath);
            if (localImage != null)
            {
                return localImage;
            }
        }

        if (File.Exists(tool.ExePath))
        {
            return ConvertIconToImageSource(System.Drawing.Icon.ExtractAssociatedIcon(tool.ExePath));
        }

        var defaultIcon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "tools.png");
        return LoadBitmapFromFile(defaultIcon)
               ?? ConvertIconToImageSource(_launchService.GetSystemIcon(DockItemType.Application));
    }

    private ImageSource? LoadDockIconFromPath(DockItem item)
    {
        try
        {
            System.Drawing.Icon? icon = null;

            if (item.Type == DockItemType.Application)
            {
                var fullPath = ResolveExecutablePath(item.Path);
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

            return ConvertIconToImageSource(icon);
        }
        catch
        {
            return null;
        }
    }

    private static string ToAbsolutePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path))
        {
            return path;
        }

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
    }

    private static string? ResolveExecutablePath(string path)
    {
        if (File.Exists(path))
        {
            return path;
        }

        var system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var candidates = new[]
        {
            Path.Combine(system32, path),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", path),
            Path.Combine(system32, "WindowsPowerShell", "v1.0", path),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysWOW64", "WindowsPowerShell", "v1.0", path)
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var pathEntries = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator) ?? Array.Empty<string>();
        foreach (var entry in pathEntries)
        {
            try
            {
                var candidate = Path.Combine(entry.Trim(), path);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            catch
            {
            }
        }

        return null;
    }

    private static ImageSource? LoadBitmapFromFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource? ConvertIconToImageSource(System.Drawing.Icon? icon)
    {
        if (icon == null)
        {
            return null;
        }

        try
        {
            using var bitmap = icon.ToBitmap();
            var hBitmap = bitmap.GetHbitmap();
            try
            {
                var source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromWidthAndHeight(32, 32));
                source.Freeze();
                return source;
            }
            finally
            {
                DeleteObject(hBitmap);
            }
        }
        catch
        {
            return null;
        }
    }

    [System.Runtime.InteropServices.DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);
}
