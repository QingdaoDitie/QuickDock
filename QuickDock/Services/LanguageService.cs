namespace QuickDock.Services;

public enum Language
{
    English,
    Chinese
}

public static class Lang
{
    public static Language CurrentLanguage { get; set; } = Language.Chinese;

    public static string T(string key)
    {
        var dict = CurrentLanguage == Language.Chinese ? Chinese : English;
        return dict.TryGetValue(key, out var value) ? value : key;
    }

    private static readonly Dictionary<string, string> Chinese = new()
    {
        { "App.Title", "QuickDock" },
        { "Tray.ToolTip", "QuickDock 快速启动" },
        { "Tray.Show", "显示" },
        { "Tray.Settings", "设置" },
        { "Tray.Exit", "退出" },
        { "Settings.Title", "QuickDock 设置" },
        { "Settings.DockItems", "Dock 项目" },
        { "Settings.Add", "添加" },
        { "Settings.Edit", "编辑" },
        { "Settings.Delete", "删除" },
        { "Settings.Up", "上移" },
        { "Settings.Down", "下移" },
        { "Settings.StartWithWindows", "开机自启动" },
        { "Settings.Save", "保存" },
        { "Settings.Cancel", "取消" },
        { "Settings.Language", "语言" },
        { "Settings.Opacity", "透明度" },
        { "Edit.Title", "编辑项目" },
        { "Edit.Name", "名称" },
        { "Edit.Type", "类型" },
        { "Edit.Path", "路径" },
        { "Edit.Arguments", "参数" },
        { "Edit.IconPath", "图标路径" },
        { "Edit.Browse", "浏览..." },
        { "Edit.BrowseIcon", "选择图标" },
        { "Edit.OK", "确定" },
        { "Edit.Cancel", "取消" },
        { "Type.Application", "应用程序" },
        { "Type.Folder", "文件夹" },
        { "Type.WebPage", "网页" },
        { "Type.Command", "命令" },
        { "Validation.NameRequired", "请输入名称" },
        { "Validation.PathRequired", "请输入路径" },
        { "Confirm.Delete", "确定删除 '{0}'?" },
        { "Error.Launch", "启动失败: {0}" },
        { "Filter.Applications", "应用程序 (*.exe)|*.exe|所有文件 (*.*)|*.*" },
        { "Filter.Icons", "图标文件 (*.ico;*.png)|*.ico;*.png|所有文件 (*.*)|*.*" },
        { "Select.Application", "选择应用程序" },
        { "Select.Folder", "选择文件夹" },
        { "Select.Icon", "选择图标" },
        { "RunAsAdmin", "以管理员身份运行" },
        { "Edit.FetchIcon", "从网址获取图标" },
        { "Edit.UseIcon", "使用" },
        { "Edit.Fetching", "正在获取图标..." },
        { "Edit.FetchSuccess", "图标获取成功" },
        { "Edit.FetchFailed", "获取失败" },
        { "Edit.FetchFailedHint", "无法从网址获取图标，请选择本地图标作为图标。" },
        { "Validation.UrlRequired", "请输入网址" },
        { "Settings.BackgroundColor", "背景颜色" },
        { "Settings.Scale", "缩放大小" },
        { "Settings.IconSize", "图标大小" },
        { "Settings.IconSpacing", "图标间距" },
        { "Settings.ShowStatusBar", "显示状态栏" },
        { "Settings.WeatherCity", "天气城市" },
        { "AppendClipboard", "追加剪贴板内容" }
    };

    private static readonly Dictionary<string, string> English = new()
    {
        { "App.Title", "QuickDock" },
        { "Tray.ToolTip", "QuickDock Launcher" },
        { "Tray.Show", "Show" },
        { "Tray.Settings", "Settings" },
        { "Tray.Exit", "Exit" },
        { "Settings.Title", "QuickDock Settings" },
        { "Settings.DockItems", "Dock Items" },
        { "Settings.Add", "Add" },
        { "Settings.Edit", "Edit" },
        { "Settings.Delete", "Delete" },
        { "Settings.Up", "Up" },
        { "Settings.Down", "Down" },
        { "Settings.StartWithWindows", "Start with Windows" },
        { "Settings.Save", "Save" },
        { "Settings.Cancel", "Cancel" },
        { "Settings.Language", "Language" },
        { "Settings.Opacity", "Opacity" },
        { "Edit.Title", "Edit Item" },
        { "Edit.Name", "Name" },
        { "Edit.Type", "Type" },
        { "Edit.Path", "Path" },
        { "Edit.Arguments", "Arguments" },
        { "Edit.IconPath", "Icon Path" },
        { "Edit.Browse", "Browse..." },
        { "Edit.BrowseIcon", "Select Icon" },
        { "Edit.OK", "OK" },
        { "Edit.Cancel", "Cancel" },
        { "Type.Application", "Application" },
        { "Type.Folder", "Folder" },
        { "Type.WebPage", "Web Page" },
        { "Type.Command", "Command" },
        { "Validation.NameRequired", "Please enter a name" },
        { "Validation.PathRequired", "Please enter a path" },
        { "Confirm.Delete", "Delete '{0}'?" },
        { "Error.Launch", "Failed to launch: {0}" },
        { "Filter.Applications", "Applications (*.exe)|*.exe|All files (*.*)|*.*" },
        { "Filter.Icons", "Icon files (*.ico;*.png)|*.ico;*.png|All files (*.*)|*.*" },
        { "Select.Application", "Select Application" },
        { "Select.Folder", "Select Folder" },
        { "Select.Icon", "Select Icon" },
        { "RunAsAdmin", "Run as Administrator" },
        { "Edit.FetchIcon", "Fetch Icon from URL" },
        { "Edit.UseIcon", "Use" },
        { "Edit.Fetching", "Fetching icon..." },
        { "Edit.FetchSuccess", "Icon fetched successfully" },
        { "Edit.FetchFailed", "Fetch failed" },
        { "Edit.FetchFailedHint", "Could not fetch icon from URL. Please select a local icon instead." },
        { "Validation.UrlRequired", "Please enter a URL" },
        { "Settings.BackgroundColor", "Background Color" },
        { "Settings.Scale", "Scale" },
        { "Settings.IconSize", "Icon Size" },
        { "Settings.IconSpacing", "Icon Spacing" },
        { "Settings.ShowStatusBar", "Show Status Bar" },
        { "Settings.WeatherCity", "Weather City" },
        { "AppendClipboard", "Append Clipboard Content" }
    };
}
