# QuickDock 

> **目标受众**: 第三方开发者及其 AI IDE  
> **项目类型**: Windows 桌面快速启动栏应用  
> **技术栈**: .NET 8.0, WPF, C#  

---

## 1. 项目全景图 (System Context)

### 1.1 技术栈版本

| 组件 | 版本 | 说明 |
|------|------|------|
| .NET SDK | 8.0 | 目标框架 `net8.0-windows` |
| WPF | 内置于 .NET 8 | Windows Presentation Foundation |
| Windows Forms | 内置于 .NET 8 | 用于 FolderBrowserDialog 和系统图标 |
| Hardcodet.NotifyIcon.Wpf | 1.1.0 | 系统托盘图标 NuGet 包 |

### 1.2 核心业务逻辑流程图

```
┌─────────────────────────────────────────────────────────────────┐
│                        应用启动流程                               │
├─────────────────────────────────────────────────────────────────┤
│  App.OnStartup()                                                │
│       │                                                         │
│       ├──→ 初始化 ConfigService (加载 config.json)               │
│       │                                                         │
│       ├──→ 初始化 AutoStartService (检查/设置开机自启)            │
│       │                                                         │
│       ├──→ 初始化 HotZoneService (启动屏幕热区监听)               │
│       │         │                                               │
│       │         └──→ DispatcherTimer (50ms 间隔轮询鼠标位置)      │
│       │                    │                                    │
│       │                    ├──→ 鼠标进入顶部热区 → 触发 HotZoneEntered │
│       │                    └──→ 鼠标离开热区 → 触发 HotZoneLeft    │
│       │                                                         │
│       ├──→ 创建 MainWindow (传入 ConfigService)                  │
│       │                                                         │
│       ├──→ 创建 TaskbarIcon (系统托盘)                           │
│       │                                                         │
│       └──→ 调用 MainWindow.ShowStartupAnimation()               │
│                   │                                             │
│                   └──→ Dock 滑入显示 1.5 秒后自动滑出隐藏          │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                      Dock 交互流程                               │
├─────────────────────────────────────────────────────────────────┤
│  用户触发                                                        │
│       │                                                         │
│       ├──→ 鼠标移至屏幕顶部热区                                   │
│       │         │                                               │
│       │         └──→ HotZoneService.HotZoneEntered 事件          │
│       │                    │                                    │
│       │                    └──→ MainWindow.Show() + SlideIn()   │
│       │                              │                          │
│       │                              └──→ 动画: Top 从负值到 0    │
│       │                                                         │
│       ├──→ 鼠标离开 Dock 区域                                    │
│       │         │                                               │
│       │         └──→ MainWindow.OnDockMouseLeave()              │
│       │                    │                                    │
│       │                    └──→ SlideOut()                      │
│       │                              │                          │
│       │                              └──→ 动画: Top 从 0 到负值   │
│       │                                    │                    │
│       │                                    └──→ Hide()          │
│       │                                                         │
│       └──→ 点击 Dock 项目                                        │
│                 │                                               │
│                 └──→ DockItemControl.OnButtonClick()            │
│                            │                                    │
│                            └──→ LaunchService.Launch(item)      │
│                                       │                         │
│                                       ├──→ Application → Process.Start │
│                                       ├──→ Folder → Explorer 打开 │
│                                       ├──→ WebPage → 默认浏览器打开 │
│                                       └──→ Command → cmd.exe 执行  │
└─────────────────────────────────────────────────────────────────┘
```

### 1.3 文件目录树结构

```
QuickDock/
├── App.xaml                          # 应用程序入口 XAML，加载全局样式
├── App.xaml.cs                       # 应用程序启动逻辑、托盘图标创建
├── MainWindow.xaml                   # 主窗口 XAML，Dock 容器
├── MainWindow.xaml.cs                # 主窗口逻辑：动画、项目加载、设置应用
├── QuickDock.csproj                  # 项目配置文件
├── ico.ico                           # 应用图标
│
├── Assets/
│   └── app.ico                       # 托盘图标资源
│
├── Controls/
│   ├── DockItemControl.xaml          # Dock 项目控件 XAML
│   └── DockItemControl.xaml.cs       # 图标加载、点击处理、尺寸计算
│
├── Models/
│   ├── AppSettings.cs                # 应用设置模型
│   └── DockItem.cs                   # Dock 项目模型
│
├── Resources/
│   └── Styles.xaml                   # 全局样式资源字典
│
├── Services/
│   ├── AutoStartService.cs           # 开机自启动服务 (注册表操作)
│   ├── ConfigService.cs              # 配置文件读写服务
│   ├── HotZoneService.cs             # 屏幕热区检测服务
│   ├── IconCacheService.cs           # 图标缓存服务 (网络/本地)
│   ├── LanguageService.cs            # 多语言服务
│   └── LaunchService.cs              # 应用启动服务
│
└── Windows/
    ├── ItemEditWindow.xaml           # 项目编辑窗口 XAML
    ├── ItemEditWindow.xaml.cs        # 项目编辑逻辑
    ├── SettingsWindow.xaml           # 设置窗口 XAML
    └── SettingsWindow.xaml.cs        # 设置窗口逻辑
```

---

## 2. 环境与依赖规范 (Setup Protocol)

### 2.1 开发环境要求

- [ ] Windows 10/11 操作系统
- [ ] .NET 8.0 SDK 或更高版本
- [ ] Visual Studio 2022 或 JetBrains Rider (可选)

### 2.2 项目配置文件 (QuickDock.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>QuickDock</AssemblyName>
    <RootNamespace>QuickDock</RootNamespace>
    <ApplicationIcon>Assets\app.ico</ApplicationIcon>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="1.1.0" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="Assets\app.ico">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

### 2.3 运行时生成的文件

程序运行后会在输出目录生成以下文件：

```
输出目录/
├── QuickDock.exe
├── config.json                       # 用户配置 (自动生成)
├── icons/                            # 自定义图标目录 (自动生成)
└── IconCache/                        # 图标缓存目录 (自动生成)
```

### 2.4 config.json 数据结构

```json
{
  "Items": [
    {
      "Id": "guid-string",
      "Name": "显示名称",
      "Type": "Application|Folder|WebPage|Command",
      "Path": "路径或URL",
      "IconPath": "相对路径或绝对路径",
      "Arguments": "命令行参数",
      "RunAsAdmin": false
    }
  ],
  "Settings": {
    "AutoStart": true,
    "HotZoneWidth": 0.3,
    "AnimationDuration": 200,
    "Language": "zh",
    "DockOpacity": 0.9,
    "BackgroundColor": "#1e1e1e",
    "Scale": 1.0,
    "IconSize": 32,
    "IconSpacing": 5
  }
}
```

---

## 3. 模块化执行路径 (Step-by-Step Execution)

### Step 1: 项目骨架搭建

- [ ] 创建 .NET 8.0 WPF 项目
- [ ] 配置 `QuickDock.csproj` 文件
- [ ] 安装 `Hardcodet.NotifyIcon.Wpf` NuGet 包
- [ ] 创建目录结构：`Assets/`, `Controls/`, `Models/`, `Resources/`, `Services/`, `Windows/`
- [ ] 准备 `app.ico` 图标文件放入 `Assets/` 目录

**验证标准**: 项目能够成功编译，无 NuGet 包还原错误

---

### Step 2: 数据模型层 (Models)

#### 2.1 创建 `Models/DockItem.cs`

```csharp
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
}
```

#### 2.2 创建 `Models/AppSettings.cs`

```csharp
namespace QuickDock.Models;

public class AppSettings
{
    public bool AutoStart { get; set; } = true;
    public double HotZoneWidth { get; set; } = 0.3;
    public int AnimationDuration { get; set; } = 200;
    public string Language { get; set; } = "zh";
    public double DockOpacity { get; set; } = 0.9;
    public string BackgroundColor { get; set; } = "#1e1e1e";
    public double Scale { get; set; } = 1.0;
    public double IconSize { get; set; } = 32;
    public double IconSpacing { get; set; } = 5;
}
```

**关键实现点**:
- `DockItem.Id` 使用 `Guid.NewGuid()` 自动生成唯一标识
- `AppSettings` 所有属性必须有默认值，确保首次运行时配置有效

**验证标准**: 模型类编译通过，属性类型正确

---

### Step 3: 服务层核心 (Services)

#### 3.1 创建 `Services/LanguageService.cs`

**关键实现点**:
- 使用静态类 `Lang` 提供全局多语言支持
- `T(string key)` 方法根据 `CurrentLanguage` 返回对应翻译
- 支持中文和英文两种语言
- 必须包含所有 UI 文本的翻译键值对

**核心翻译键列表**:
```
App.Title, Tray.ToolTip, Tray.Show, Tray.Settings, Tray.Exit,
Settings.Title, Settings.DockItems, Settings.Add, Settings.Edit,
Settings.Delete, Settings.Up, Settings.Down, Settings.StartWithWindows,
Settings.Save, Settings.Cancel, Settings.Language, Settings.Opacity,
Settings.BackgroundColor, Settings.Scale, Settings.IconSize, Settings.IconSpacing,
Edit.Title, Edit.Name, Edit.Type, Edit.Path, Edit.Arguments, Edit.IconPath,
Edit.Browse, Edit.BrowseIcon, Edit.OK, Edit.Cancel, Edit.FetchIcon,
Type.Application, Type.Folder, Type.WebPage, Type.Command,
Validation.NameRequired, Validation.PathRequired, Validation.UrlRequired,
Confirm.Delete, Error.Launch, RunAsAdmin
```

#### 3.2 创建 `Services/ConfigService.cs`

**关键实现点**:
- 使用 `System.Text.Json` 进行序列化/反序列化
- 配置文件路径：`AppDomain.CurrentDomain.BaseDirectory + "config.json"`
- 图标目录路径：`AppDomain.CurrentDomain.BaseDirectory + "icons"`
- **相对路径转换**: 保存时将绝对路径转为相对路径，加载时转回绝对路径
- 默认配置包含 CMD 和 PowerShell 两个示例项目

**核心方法**:
```csharp
public void Load()           // 加载配置，失败时调用 LoadDefault()
public void Save()           // 保存配置，自动转换相对路径
public void AddItem(DockItem item)
public void RemoveItem(string id)
public void UpdateItem(DockItem item)
public void MoveItem(int oldIndex, int newIndex)
public string ToRelativePath(string? absolutePath)
public string ToAbsolutePath(string? relativePath)
```

#### 3.3 创建 `Services/AutoStartService.cs`

**关键实现点**:
- 使用注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
- `Enable()`: 写入注册表键值
- `Disable()`: 删除注册表键值
- 使用 `Environment.ProcessPath` 获取当前 exe 路径

#### 3.4 创建 `Services/LaunchService.cs`

**关键实现点**:
- 根据 `DockItemType` 分发不同的启动逻辑
- `Application`: 使用 `Process.Start()` + `UseShellExecute = true`
- `Folder`: 使用 `Process.Start()` 打开文件夹
- `WebPage`: 自动添加 `https://` 前缀
- `Command`: 使用 `cmd.exe /c {command}` 执行
- `RunAsAdmin`: 设置 `startInfo.Verb = "runas"`
- `GetFileIcon()`: 使用 `Icon.ExtractAssociatedIcon()` 提取文件图标
- `GetSystemIcon()`: 使用 `SystemIcons.GetStockIcon()` 获取系统图标

#### 3.5 创建 `Services/HotZoneService.cs`

**关键实现点**:
- 使用 `DispatcherTimer` 每 50ms 检测鼠标位置
- 使用 Win32 API `GetCursorPos()` 获取鼠标坐标
- 使用 `MonitorFromPoint()` 和 `GetMonitorInfo()` 获取屏幕边界
- **热区计算逻辑**:
  - 热区位于屏幕顶部中央
  - 热区宽度 = 屏幕宽度 × `HotZoneWidth` (默认 0.3)
  - 触发条件: `Y <= 屏幕顶部 + 5` 且 `X 在热区水平范围内`
- 实现 `IDisposable` 接口

**Win32 API 声明**:
```csharp
[DllImport("user32.dll")]
private static extern bool GetCursorPos(out POINT lpPoint);

[DllImport("user32.dll")]
private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

[DllImport("user32.dll")]
private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
```

#### 3.6 创建 `Services/IconCacheService.cs`

**关键实现点**:
- 单例模式实现
- 缓存目录：`AppDomain.CurrentDomain.BaseDirectory + "IconCache"`
- `DownloadIconAsync()`: 从 `https://icon.bqb.cool?url={encodedUrl}` 获取网站图标
- `CacheLocalIcon()`: 复制本地图标到缓存目录
- 使用 SHA256 哈希生成缓存文件名

**验证标准**: 所有服务类编译通过，无依赖错误

---

### Step 4: 全局样式资源 (Resources)

#### 4.1 创建 `Resources/Styles.xaml`

**关键样式定义**:

| 样式名称 | 目标类型 | 关键属性 |
|---------|---------|---------|
| `BackgroundBrush` | SolidColorBrush | Color=#1e1e1e |
| `SecondaryBackgroundBrush` | SolidColorBrush | Color=#2d2d2d |
| `HoverBackgroundBrush` | SolidColorBrush | Color=#3c3c3c |
| `BorderBrush` | SolidColorBrush | Color=#3c3c3c |
| `AccentBrush` | SolidColorBrush | Color=#007acc |
| `TextBrush` | SolidColorBrush | Color=#cccccc |
| `DockPanelStyle` | Border | CornerRadius=8, BorderThickness=1 |
| `DockItemButtonStyle` | Button | Width=50, Height=50, CornerRadius=6 |
| `SettingsButtonStyle` | Button | CornerRadius=4, Padding=10,5 |
| `TextBoxStyle` | TextBox | CornerRadius=4, Padding=8,5 |
| `WindowStyle` | Window | Background=BackgroundBrush |

**验证标准**: XAML 无语法错误，资源键名正确

---

### Step 5: 应用程序入口 (App)

#### 5.1 创建 `App.xaml`

```xaml
<Application x:Class="QuickDock.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles.xaml"/>
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

**关键点**: `ShutdownMode="OnExplicitShutdown"` 确保关闭窗口不会退出应用

#### 5.2 创建 `App.xaml.cs`

**启动流程实现**:

1. 注册异常处理器 (`DispatcherUnhandledException`, `AppDomain.UnhandledException`)
2. 初始化 `ConfigService`
3. 初始化 `AutoStartService`，如果 `Settings.AutoStart` 为 true 则启用
4. 初始化 `HotZoneService`，订阅 `HotZoneEntered` 和 `HotZoneLeft` 事件
5. 创建 `MainWindow`，订阅 `MouseLeave` 事件
6. 创建 `TaskbarIcon` (系统托盘)
7. 调用 `MainWindow.ShowStartupAnimation()`

**托盘菜单项**:
- 显示 (Show)
- 设置 (Settings)
- 分隔线
- 退出 (Exit)

**验证标准**: 应用启动后托盘图标显示，Dock 显示启动动画后隐藏

---

### Step 6: 主窗口 (MainWindow)

#### 6.1 创建 `MainWindow.xaml`

**窗口属性**:
```xaml
WindowStyle="None"
AllowsTransparency="True"
Background="Transparent"
Topmost="True"
ShowInTaskbar="False"
ResizeMode="NoResize"
SizeToContent="WidthAndHeight"
```

**布局结构**:
```
Grid (MainGrid)
  └── Border (DockBorder) [Style={StaticResource DockPanelStyle}]
        └── ItemsControl (DockItems)
              └── StackPanel (Horizontal)
                    └── DockItemControl (DataTemplate)
```

#### 6.2 创建 `MainWindow.xaml.cs`

**核心字段**:
```csharp
private readonly ConfigService _configService;
private readonly LaunchService _launchService;
private bool _isHidden = true;
private bool _isAnimating;
private double _baseWindowHeight = 70;
private bool _hasShownStartupAnimation = false;
private const int DesiredFrameRate = 120;
```

**关键方法实现**:

| 方法 | 功能 | 关键逻辑 |
|------|------|---------|
| `ShowStartupAnimation()` | 启动动画 | 显示窗口 → SlideIn() → 延迟 1.5s → SlideOut() |
| `SlideIn()` | 滑入动画 | Top 从 `-scaledHeight` 到 0，CubicEase.EaseOut |
| `SlideOut()` | 滑出动画 | Top 从 0 到 `-scaledHeight`，CubicEase.EaseIn，完成后 Hide() |
| `PositionWindow()` | 定位窗口 | 水平居中，Top 设为负值 |
| `ApplySettings()` | 应用设置 | 调用所有 Apply* 方法 |
| `ApplyScale()` | 应用缩放 | `MainGrid.LayoutTransform = new ScaleTransform(scale, scale)` |
| `ApplyIconSpacing()` | 应用间距 | 使用 `ItemContainerStyle` 设置 Margin |
| `GetScaledHeight()` | 计算高度 | `_baseWindowHeight * Scale` |

**动画关键代码**:
```csharp
var animation = new DoubleAnimation
{
    From = startValue,
    To = endValue,
    Duration = TimeSpan.FromSeconds(0.25),
    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
    FillBehavior = FillBehavior.HoldEnd
};
Timeline.SetDesiredFrameRate(animation, DesiredFrameRate); // 120fps
BeginAnimation(TopProperty, animation);
```

**验证标准**: 
- 鼠标移至屏幕顶部热区，Dock 滑入显示
- 鼠标离开 Dock，Dock 滑出隐藏
- 启动时显示 1.5 秒后自动隐藏

---

### Step 7: Dock 项目控件 (DockItemControl)

#### 7.1 创建 `Controls/DockItemControl.xaml`

**布局结构**:
```
UserControl
  └── Button [Style={StaticResource DockItemButtonStyle}]
        └── Grid
              ├── Border (IconBorder)
              │     └── Rectangle (IconRect) [RadiusX=6, RadiusY=6]
              │           └── ImageBrush (ItemIconBrush)
              └── TextBlock (FallbackText) [Visibility=Collapsed]
```

#### 7.2 创建 `Controls/DockItemControl.xaml.cs`

**静态共享配置**:
```csharp
public static ConfigService? SharedConfigService;
```

**图标加载优先级**:
1. `item.IconPath` 指定的图标文件
2. `item.Path` 对应的文件/系统图标
3. 根据 `item.Type` 的默认系统图标
4. 显示名称首字母作为 Fallback

**路径解析逻辑** (`ResolveFullPath`):
1. 直接检查文件是否存在
2. 检查 `System32` 目录
3. 检查 `SysWOW64` 目录
4. 检查 `WindowsPowerShell\v1.0` 目录
5. 遍历 `PATH` 环境变量

**图标转换**:
```csharp
// GDI+ Icon → WPF BitmapSource
var bitmap = icon.ToBitmap();
var hBitmap = bitmap.GetHbitmap();
var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, ...);
DeleteObject(hBitmap); // 必须释放 GDI 资源
```

**验证标准**: 图标正确显示，点击能启动对应项目

---

### Step 8: 设置窗口 (SettingsWindow)

#### 8.1 创建 `Windows/SettingsWindow.xaml`

**布局结构**:
- Row 0: 标题 "Dock Items"
- Row 1: ListBox (项目列表) + 操作按钮组 (Add/Edit/Delete/Up/Down)
- Row 2: AutoStart CheckBox
- Row 3: Language ComboBox
- Row 4: BackgroundColor TextBox + Preview Border
- Row 5: Opacity Slider + Value TextBlock
- Row 6: Scale Slider + Value TextBlock
- Row 7: IconSize Slider + Value TextBlock
- Row 8: IconSpacing Slider + Value TextBlock
- Row 9: Save/Cancel 按钮

#### 8.2 创建 `Windows/SettingsWindow.xaml.cs`

**关键实现点**:
- 使用 `_initialized` 标志防止初始化时触发事件
- Slider 的 `ValueChanged` 事件中检查 `_initialized`
- 语言切换后调用 `ApplyLanguage()` 更新所有 UI 文本
- 保存时先更新 `_configService.Items`，再调用 `Save()`

**Slider 初始化**:
```csharp
// 必须在 InitializeComponent() 之后设置 Value
var opacity = _configService.Settings.DockOpacity;
OpacitySlider.Value = opacity;
OpacityValue.Text = $"{(int)(opacity * 100)}%";

_initialized = true; // 最后设置标志
```

**验证标准**: 所有设置能正确保存和加载

---

### Step 9: 项目编辑窗口 (ItemEditWindow)

#### 9.1 创建 `Windows/ItemEditWindow.xaml`

**布局结构**:
- Name TextBox
- Type ComboBox
- Path TextBox + Browse Button
- Arguments TextBox
- IconPath TextBox + BrowseIcon Button
- IconPreviewArea Border (Visibility=Collapsed)
- FetchIcon Button (Visibility=Collapsed, 仅 WebPage 类型)
- RunAsAdmin CheckBox (Visibility=Collapsed, 仅 Application 类型)
- OK/Cancel 按钮

#### 9.2 创建 `Windows/ItemEditWindow.xaml.cs`

**关键实现点**:
- 构造函数支持 `new ItemEditWindow()` (新建) 和 `new ItemEditWindow(item)` (编辑)
- `OnTypeChanged()`: 根据类型显示/隐藏相关控件
- `OnBrowseClick()`: Application 用 OpenFileDialog，Folder 用 FolderBrowserDialog
- `OnFetchIconClick()`: 异步从 URL 获取图标，使用 `IconCacheService.Instance.DownloadIconAsync()`
- `OnOkClick()`: 验证必填字段，设置 `DialogResult = true`

**图标获取 API**:
```
GET https://icon.bqb.cool?url={encodedUrl}
返回: PNG 图标二进制数据
```

**验证标准**: 
- 能添加/编辑/删除项目
- 网页类型能自动获取图标
- 管理员权限选项正常工作

---

### Step 10: 最终集成与测试

- [ ] 编译项目，确保无错误无警告
- [ ] 运行应用，验证启动动画
- [ ] 测试热区触发
- [ ] 测试添加/编辑/删除项目
- [ ] 测试各类型项目启动
- [ ] 测试设置保存和加载
- [ ] 测试开机自启动
- [ ] 测试语言切换
- [ ] 测试托盘菜单

---

## 4. AI IDE 指引 (Agentic Instructions)

### 4.1 核心算法不可修改

1. **热区检测算法**: 必须使用 Win32 API 获取精确的屏幕边界，不能使用 WPF 的 `SystemParameters`
2. **动画帧率**: 必须使用 `Timeline.SetDesiredFrameRate(animation, 120)` 确保 120fps
3. **相对路径转换**: 保存时必须转换为相对路径，加载时转回绝对路径
4. **图标加载优先级**: IconPath → Path 图标 → 类型默认图标 → Fallback 文字

### 4.2 样式规范

- 所有颜色使用 `Styles.xaml` 中定义的资源
- 按钮圆角统一使用 `CornerRadius=4` (小按钮) 或 `CornerRadius=6` (图标按钮)
- Dock 容器圆角 `CornerRadius=8`
- 文本颜色使用 `TextBrush` (#cccccc)

### 4.3 命名规范

| 类型 | 命名规范 | 示例 |
|------|---------|------|
| 窗口 | PascalCase + Window | `SettingsWindow` |
| 控件 | PascalCase + Control | `DockItemControl` |
| 服务 | PascalCase + Service | `ConfigService` |
| 模型 | PascalCase | `DockItem`, `AppSettings` |
| XAML 元素 | PascalCase | `DockBorder`, `ItemsListBox` |
| 私有字段 | _camelCase | `_configService`, `_isHidden` |

### 4.4 验证标准

| 阶段 | 验证方法 |
|------|---------|
| Step 1-2 | 编译通过 |
| Step 3 | 服务类单元测试或编译验证 |
| Step 4 | XAML 设计器预览正常 |
| Step 5 | 应用启动，托盘图标显示 |
| Step 6 | 热区触发，动画流畅 |
| Step 7 | 图标显示，点击启动 |
| Step 8-9 | 设置保存/加载正确 |
| Step 10 | 全功能测试通过 |

---

## 5. 避坑指南 (Common Pitfalls)

### 5.1 动画相关问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| 动画卡顿 | 默认帧率低 | 使用 `Timeline.SetDesiredFrameRate(animation, 120)` |
| 动画高度不正确 | 使用固定值 | 使用 `GetScaledHeight()` 动态计算 |
| 动画完成后窗口不隐藏 | Completed 事件未触发 | 确保 `FillBehavior = HoldEnd` 并在 Completed 中调用 Hide() |
| 启动动画不显示 | Show()/Hide() 冲突 | 只在 `ShowStartupAnimation()` 中调用 Show() |

### 5.2 设置相关问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| Slider 初始值显示错误 | ValueChanged 在初始化前触发 | 使用 `_initialized` 标志，在设置 Value 后才设为 true |
| 背景颜色预览不更新 | 只更新了预览控件 | 同时更新 `_configService.Settings.BackgroundColor` |
| 缩放只影响图标 | 动画使用固定高度 | 使用 `GetScaledHeight()` 计算动画高度 |
| 图标大小不生效 | DockItemControl 创建新 ConfigService | 使用静态 `SharedConfigService` 属性 |
| 图标间距不生效 | ItemContainerStyle 未设置 | 在 `ApplyIconSpacing()` 中创建 Style 并设置 Margin |

### 5.3 图标相关问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| 图标显示模糊 | 未使用高 DPI 支持 | 使用 `BitmapCacheOption.OnLoad` |
| GDI 资源泄漏 | hBitmap 未释放 | 调用 `DeleteObject(hBitmap)` |
| 系统路径找不到 | 只检查当前目录 | 实现 `ResolveFullPath()` 搜索多个路径 |

### 5.4 配置相关问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| 路径在其他电脑无效 | 保存了绝对路径 | 实现 `ToRelativePath()` 和 `ToAbsolutePath()` |
| 配置文件损坏 | JSON 格式错误 | 使用 try-catch，失败时调用 `LoadDefault()` |

### 5.5 热区检测问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| 多显示器热区不准 | 使用主屏幕坐标 | 使用 `MonitorFromPoint()` 获取当前显示器 |
| 热区触发延迟 | Timer 间隔太长 | 使用 50ms 间隔 |

### 5.6 线程安全问题

| 问题 | 原因 | 解决方案 |
|------|------|---------|
| 跨线程访问 UI | Timer 在后台线程 | 使用 `Dispatcher.Invoke()` |
| 异步操作未 await | 图标下载是异步的 | 使用 `async/await` 模式 |

---

## 6. 附录

### 6.1 NuGet 依赖

```xml
<PackageReference Include="Hardcodet.NotifyIcon.Wpf" Version="1.1.0" />
```

### 6.2 Win32 API 参考

```csharp
// 获取鼠标位置
[DllImport("user32.dll")]
private static extern bool GetCursorPos(out POINT lpPoint);

// 获取显示器句柄
[DllImport("user32.dll")]
private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

// 获取显示器信息
[DllImport("user32.dll")]
private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

// 释放 GDI 对象
[DllImport("gdi32.dll")]
private static extern bool DeleteObject(IntPtr hObject);
```

### 6.3 关键常量

| 常量 | 值 | 说明 |
|------|-----|------|
| `DesiredFrameRate` | 120 | 动画帧率 |
| `HotZoneCheckInterval` | 50ms | 热区检测间隔 |
| `StartupAnimationDuration` | 1500ms | 启动动画显示时长 |
| `AnimationDuration` | 250ms | 滑入滑出动画时长 |
| `BaseWindowHeight` | 70 | 基础窗口高度 |

---

**文档版本**: 1.0  
**最后更新**: 2024
