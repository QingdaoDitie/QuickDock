namespace QuickDock.Models;

public enum DockItemType
{
    Application,
    Folder,
    WebPage,
    Command
}

public class DockItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public DockItemType Type { get; set; }
    public string Path { get; set; } = "";
    public string? IconPath { get; set; }
    public string? Arguments { get; set; }
    public bool RunAsAdmin { get; set; }
    public bool AppendClipboard { get; set; }
}
