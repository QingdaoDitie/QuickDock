using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using QuickDock.Models;

namespace QuickDock.Services;

public class LaunchService
{
    public void Launch(DockItem item)
    {
        try
        {
            switch (item.Type)
            {
                case DockItemType.Application:
                    LaunchApplication(item.Path, item.Arguments, item.RunAsAdmin);
                    break;
                case DockItemType.Folder:
                    LaunchFolder(item.Path);
                    break;
                case DockItemType.WebPage:
                    LaunchWebPage(item.Path);
                    break;
                case DockItemType.Command:
                    LaunchCommand(item.Path);
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                string.Format(Lang.T("Error.Launch"), ex.Message), 
                "Error", 
                System.Windows.MessageBoxButton.OK, 
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void LaunchApplication(string path, string? arguments, bool runAsAdmin)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        };
        
        if (runAsAdmin)
        {
            startInfo.Verb = "runas";
        }
        
        if (!string.IsNullOrEmpty(arguments))
        {
            startInfo.Arguments = arguments;
        }
        Process.Start(startInfo);
    }

    private void LaunchFolder(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    private void LaunchWebPage(string url)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private void LaunchCommand(string command)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            UseShellExecute = true
        });
    }

    public System.Drawing.Icon? GetSystemIcon(DockItemType type)
    {
        return type switch
        {
            DockItemType.Folder => System.Drawing.SystemIcons.GetStockIcon(System.Drawing.StockIconId.Folder),
            DockItemType.WebPage => System.Drawing.SystemIcons.GetStockIcon(System.Drawing.StockIconId.World),
            DockItemType.Command => System.Drawing.SystemIcons.GetStockIcon(System.Drawing.StockIconId.Application),
            DockItemType.Application => System.Drawing.SystemIcons.GetStockIcon(System.Drawing.StockIconId.Application),
            _ => System.Drawing.SystemIcons.GetStockIcon(System.Drawing.StockIconId.Application)
        };
    }

    public System.Drawing.Icon? GetFileIcon(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return System.Drawing.Icon.ExtractAssociatedIcon(path);
            }
            else if (Directory.Exists(path))
            {
                return System.Drawing.SystemIcons.GetStockIcon(System.Drawing.StockIconId.Folder);
            }
        }
        catch { }
        return null;
    }
}
