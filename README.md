# QuickDock

<p align="center">
  <img src="QuickDock/Assets/app.ico" alt="QuickDock Logo" width="128" height="128">
</p>

轻量级 Windows 快捷启动工具，基于 WPF 开发，支持自定义程序快捷方式、天气显示、系统资源监控。

## 功能特性

### 快捷启动
- 支持添加应用程序、文件夹、网页、命令四种类型
- 拖拽排序，自由调整位置
- 支持以管理员权限运行
- 自动提取程序图标，支持自定义图标

### 天气显示
- 实时显示当前城市天气状况
- 支持手动刷新
- 显示温度、天气状况

### 系统资源监控
- 实时显示 CPU 利用率
- 实时显示内存使用率
- 实时显示 GPU 利用率（3D 引擎）

### 热区触发
- 鼠标移至屏幕底部指定区域自动显示
- 鼠标移开后自动隐藏
- 可调节热区宽度

### 系统托盘
- 最小化到系统托盘运行
- 右键菜单快速操作
- 支持开机自启动

## 安装

### 环境要求
- Windows 10/11
- .NET 8.0 Runtime

### 从源码编译

```bash
git clone https://github.com/QingdaoDitie/QuickDock.git
cd QuickDock/QuickDock
dotnet build -c Release
```

编译完成后，可执行文件位于 `bin/Release/net8.0-windows/QuickDock.exe`

## 使用说明

### 基本操作

1. **启动程序**：双击 QuickDock.exe 运行
2. **显示 Dock**：将鼠标移至屏幕底部热区
3. **隐藏 Dock**：鼠标移开 Dock 区域
4. **打开设置**：右键点击托盘图标，选择"设置"

### 添加快捷方式

1. 打开设置窗口
2. 点击"添加项目"按钮
3. 填写以下信息：
   - **名称**：显示名称
   - **类型**：应用程序/文件夹/网页/命令
   - **路径**：程序路径、文件夹路径或网址
   - **参数**：启动参数（可选）
   - **管理员权限**：是否以管理员身份运行

### 配置选项

| 选项 | 说明 | 默认值 |
|------|------|--------|
| 开机自启 | 系统启动时自动运行 | 开启 |
| 热区宽度 | 触发显示的屏幕底部区域比例 | 30% |
| 动画时长 | 显示/隐藏动画时间（毫秒） | 200 |
| 语言 | 界面语言（中文/英文） | 中文 |
| 透明度 | Dock 窗口透明度 | 0.9 |
| 缩放比例 | 整体缩放比例 | 1.0 |
| 图标大小 | 快捷方式图标尺寸 | 32px |
| 图标间距 | 快捷方式之间的间距 | 5px |
| 显示状态栏 | 是否显示天气和资源监控 | 开启 |
| 城市 | 天气查询城市 | 空 |

### 配置文件

配置文件位于程序目录下的 `config.json`，包含：

```json
{
  "Items": [
    {
      "Id": "唯一标识",
      "Name": "显示名称",
      "Type": "Application/Folder/WebPage/Command",
      "Path": "路径或网址",
      "IconPath": "图标路径",
      "Arguments": "启动参数",
      "RunAsAdmin": false
    }
  ],
  "Settings": {
    "AutoStart": true,
    "HotZoneWidth": 0.3,
    "AnimationDuration": 200,
    "Language": "zh",
    "DockOpacity": 0.9,
    "Scale": 1.0,
    "IconSize": 32,
    "IconSpacing": 5,
    "ShowStatusBar": true,
    "WeatherCity": "北京"
  }
}
```

## 技术栈

- **.NET 8** - 运行时框架
- **WPF** - 用户界面框架
- **C#** - 开发语言
- **Hardcodet.NotifyIcon.Wpf** - 系统托盘支持
- **System.Management** - 系统信息获取

## 项目结构

```
QuickDock/
├── Assets/
│   └── app.ico              # 应用程序图标
├── Controls/
│   ├── DockItemControl.xaml # 快捷方式控件
│   └── StatusControl.xaml   # 状态栏控件
├── Models/
│   ├── AppSettings.cs       # 应用设置模型
│   ├── DockItem.cs          # 快捷方式模型
│   └── WeatherData.cs       # 天气数据模型
├── Services/
│   ├── AutoStartService.cs  # 开机自启服务
│   ├── ConfigService.cs     # 配置管理服务
│   ├── HotZoneService.cs    # 热区检测服务
│   ├── IconCacheService.cs  # 图标缓存服务
│   ├── LanguageService.cs   # 多语言服务
│   ├── LaunchService.cs     # 程序启动服务
│   ├── StatusService.cs     # 状态信息服务
│   ├── SystemResourceService.cs # 系统资源监控
│   └── WeatherService.cs    # 天气查询服务
├── Windows/
│   ├── ItemEditWindow.xaml  # 项目编辑窗口
│   └── SettingsWindow.xaml  # 设置窗口
├── App.xaml                 # 应用程序入口
├── MainWindow.xaml          # 主窗口
└── QuickDock.csproj         # 项目文件
```

## 许可证

[MIT License](LICENSE)

本项目采用 MIT 许可证开源，您可以自由使用、修改、分发本软件，包括商业用途。唯一要求是在副本中包含原始版权声明和许可证声明。
