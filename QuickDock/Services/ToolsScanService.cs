using System.IO;
using System.Linq;
using QuickDock.Models;

namespace QuickDock.Services;

public class ToolsScanService
{
    private static readonly string[] ExcludeKeywords = {
        "uninstall", "unins", "setup", "install",
        "update", "updater", "uninst", "_setup", "_install"
    };

    public ScanResult Scan(string rootPath)
    {
        var confirmed = new List<ToolItem>();
        var needsConfirm = new List<PendingToolFolder>();

        if (!Directory.Exists(rootPath))
            return new ScanResult { Error = "路径不存在" };

        try
        {
            foreach (var exePath in Directory.GetFiles(rootPath, "*.exe"))
            {
                if (IsExcluded(exePath)) continue;
                confirmed.Add(CreateToolItem(exePath, isConfirmed: true));
            }
        }
        catch { }

        try
        {
            foreach (var folder in Directory.GetDirectories(rootPath))
            {
                List<string> exeFiles;
                try
                {
                    exeFiles = Directory.GetFiles(folder, "*.exe")
                        .Where(f => !IsExcluded(f))
                        .ToList();
                }
                catch { continue; }

                if (exeFiles.Count == 0)
                    continue;

                if (exeFiles.Count == 1)
                {
                    confirmed.Add(CreateToolItem(exeFiles[0], isConfirmed: true, sourceFolder: folder));
                }
                else
                {
                    needsConfirm.Add(new PendingToolFolder
                    {
                        FolderPath = folder,
                        FolderName = Path.GetFileName(folder),
                        Candidates = exeFiles
                    });
                }
            }
        }
        catch { }

        for (int i = 0; i < confirmed.Count; i++)
        {
            confirmed[i].Order = i;
        }

        return new ScanResult
        {
            ConfirmedItems = confirmed,
            PendingFolders = needsConfirm
        };
    }

    private bool IsExcluded(string exePath)
    {
        var name = Path.GetFileNameWithoutExtension(exePath).ToLower();
        return ExcludeKeywords.Any(k => name.Contains(k));
    }

    private ToolItem CreateToolItem(string exePath, bool isConfirmed, string? sourceFolder = null)
    {
        return new ToolItem
        {
            DisplayName = Path.GetFileNameWithoutExtension(exePath),
            ExePath = exePath,
            SourceFolder = sourceFolder,
            IsConfirmed = isConfirmed,
            Order = 0
        };
    }
}
