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
            var clipboardContent = item.AppendClipboard ? GetClipboardText() : null;
            
            switch (item.Type)
            {
                case DockItemType.Application:
                    LaunchApplication(item.Path, item.Arguments, item.RunAsAdmin, clipboardContent);
                    break;
                case DockItemType.Folder:
                    LaunchFolder(item.Path);
                    break;
                case DockItemType.WebPage:
                    LaunchWebPage(item.Path, clipboardContent);
                    break;
                case DockItemType.Command:
                    LaunchCommand(item.Path, clipboardContent);
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

    private void LaunchApplication(string path, string? arguments, bool runAsAdmin, string? clipboardContent)
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
        
        var args = arguments ?? "";
        if (!string.IsNullOrEmpty(clipboardContent))
        {
            args = string.IsNullOrEmpty(args) 
                ? $"\"{clipboardContent}\"" 
                : $"{args} \"{clipboardContent}\"";
        }
        
        if (!string.IsNullOrEmpty(args))
        {
            startInfo.Arguments = args;
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

    private void LaunchWebPage(string url, string? clipboardContent)
    {
        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }
        
        if (!string.IsNullOrEmpty(clipboardContent))
        {
            var encodedContent = Uri.EscapeDataString(clipboardContent);
            if (url.Contains("?"))
            {
                url = $"{url}&q={encodedContent}";
            }
            else
            {
                url = $"{url}?q={encodedContent}";
            }
        }
        
        Process.Start(new ProcessStartInfo
        {
            FileName = url,
            UseShellExecute = true
        });
    }

    private void LaunchCommand(string command, string? clipboardContent)
    {
        if (!string.IsNullOrEmpty(clipboardContent))
        {
            command = $"{command} \"{clipboardContent}\"";
        }
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c {command}",
            UseShellExecute = true
        });
    }

    private string? GetClipboardText()
    {
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                return System.Windows.Clipboard.GetText();
            }
        }
        catch { }
        return null;
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
