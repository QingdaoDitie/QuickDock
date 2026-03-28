# QuickDock 功能增强计划

## 需求概述

1. **图标圆角** - 图标本身添加圆角效果
2. **IconPath 相对路径** - 配置文件中使用相对路径
3. **开机启动修复** - 确保注册表开机启动正常工作
4. **四个新配置项**：
   - 背景颜色
   - 整体缩放大小
   - 图标大小
   - 图标间距

---

## 实现步骤

### 第一部分：图标圆角效果

**修改文件**: `Controls/DockItemControl.xaml`

- 为 Image 控件添加圆角裁剪
- 使用 `Border` 包裹 `Image`，设置 `CornerRadius`
- 或使用 `Image.Clip` 的 `RectangleGeometry` 实现圆角

```xaml
<Border CornerRadius="6" ClipToBounds="True">
    <Image x:Name="ItemIcon" .../>
</Border>
```

---

### 第二部分：IconPath 相对路径支持

**修改文件**: 
- `Services/ConfigService.cs` - 添加路径转换方法
- `Services/IconCacheService.cs` - 修改缓存路径处理
- `Controls/DockItemControl.xaml.cs` - 加载时转换为绝对路径

**实现逻辑**:
1. 保存时：将绝对路径转换为相对于 exe 目录的相对路径
2. 加载时：将相对路径转换为绝对路径
3. 支持两种格式：
   - 相对路径：`icons/chatgpt.png` 或 `IconCache/xxx.png`
   - 绝对路径：保持原样（兼容旧配置）

```csharp
// ConfigService.cs
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
```

---

### 第三部分：开机启动修复

**修改文件**: `Services/AutoStartService.cs`

**问题分析**:
- 当前 `_exePath` 可能获取不正确
- `Environment.ProcessPath` 在某些情况下可能为 null

**修复方案**:
```csharp
public AutoStartService()
{
    // 确保获取正确的 exe 完整路径
    _exePath = Environment.ProcessPath 
        ?? Assembly.GetExecutingAssembly().Location
        ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QuickDock.exe");
}

public void Enable()
{
    using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
    // 添加引号确保路径中有空格时也能正常工作
    key?.SetValue(AppName, $"\"{_exePath}\"");
}
```

---

### 第四部分：新增四个配置项

#### 4.1 修改 AppSettings 模型

**文件**: `Models/AppSettings.cs`

```csharp
public class AppSettings
{
    public bool AutoStart { get; set; } = true;
    public double HotZoneWidth { get; set; } = 0.3;
    public int AnimationDuration { get; set; } = 200;
    public string Language { get; set; } = "zh";
    public double DockOpacity { get; set; } = 0.9;
    
    // 新增配置项
    public string BackgroundColor { get; set; } = "#1e1e1e";  // 背景颜色
    public double Scale { get; set; } = 1.0;                   // 整体缩放 (0.5 - 2.0)
    public double IconSize { get; set; } = 32;                 // 图标大小 (16 - 64)
    public double IconSpacing { get; set; } = 5;               // 图标间距 (0 - 20)
}
```

#### 4.2 修改设置窗口 UI

**文件**: `Windows/SettingsWindow.xaml`

添加四个新的设置控件：
- 背景颜色：ColorPicker 或 TextBox (输入十六进制颜色)
- 缩放大小：Slider (0.5 - 2.0)
- 图标大小：Slider (16 - 64)
- 图标间距：Slider (0 - 20)

#### 4.3 修改 Dock 显示逻辑

**文件**: `MainWindow.xaml` 和 `MainWindow.xaml.cs`

- 应用背景颜色到 DockPanelStyle
- 应用缩放到整个 Window 或 Border
- 动态调整 DockItemControl 的尺寸
- 动态调整 ItemsControl 中项目间距

#### 4.4 修改 DockItemControl

**文件**: `Controls/DockItemControl.xaml`

- 绑定图标大小到设置值
- 从 MainWindow 传递设置参数

#### 4.5 更新语言包

**文件**: `Services/LanguageService.cs`

添加新的翻译键：
- `Settings.BackgroundColor` - 背景颜色 / Background Color
- `Settings.Scale` - 缩放大小 / Scale
- `Settings.IconSize` - 图标大小 / Icon Size
- `Settings.IconSpacing` - 图标间距 / Icon Spacing

---

## 执行顺序

1. ✅ 图标圆角效果 (DockItemControl.xaml)
2. ✅ IconPath 相对路径支持 (ConfigService.cs, DockItemControl.xaml.cs)
3. ✅ 开机启动修复 (AutoStartService.cs)
4. ✅ 新增配置模型 (AppSettings.cs)
5. ✅ 更新设置窗口 UI (SettingsWindow.xaml, SettingsWindow.xaml.cs)
6. ✅ 更新语言包 (LanguageService.cs)
7. ✅ 应用配置到主窗口 (MainWindow.xaml, MainWindow.xaml.cs)
8. ✅ 应用配置到 DockItemControl (DockItemControl.xaml)
9. ✅ 编译测试

---

## 文件修改清单

| 文件 | 修改内容 |
|------|----------|
| `Controls/DockItemControl.xaml` | 图标圆角、动态尺寸绑定 |
| `Services/ConfigService.cs` | 相对路径转换方法 |
| `Services/AutoStartService.cs` | 修复 exe 路径获取 |
| `Models/AppSettings.cs` | 新增4个配置属性 |
| `Windows/SettingsWindow.xaml` | 新增4个设置控件 |
| `Windows/SettingsWindow.xaml.cs` | 新增配置读写逻辑 |
| `Services/LanguageService.cs` | 新增翻译文本 |
| `MainWindow.xaml` | 动态背景、缩放、间距 |
| `MainWindow.xaml.cs` | 应用新配置、刷新方法 |
