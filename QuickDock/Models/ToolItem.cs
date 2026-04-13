namespace QuickDock.Models;

public class ToolItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DisplayName { get; set; } = string.Empty;
    public string ExePath { get; set; } = string.Empty;
    public string? SourceFolder { get; set; }
    public bool IsConfirmed { get; set; } = true;
    public string? CustomIconPath { get; set; }
    public int Order { get; set; }
}

public class PendingToolFolder
{
    public string FolderPath { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public List<string> Candidates { get; set; } = new();
    public string? SelectedExePath { get; set; }
}

public class ScanResult
{
    public List<ToolItem> ConfirmedItems { get; set; } = new();
    public List<PendingToolFolder> PendingFolders { get; set; } = new();
    public string? Error { get; set; }
}
