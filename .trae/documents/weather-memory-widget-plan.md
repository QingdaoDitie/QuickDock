# 天气与内存显示组件实现计划

## 功能概述
在 Dock 左侧添加一个长方形组件，显示当前天气和内存占用信息。

## 用户需求确认
- **城市获取**：自动获取位置（通过 IP 定位）
- **内存显示格式**：具体数值显示（如：8.2GB / 16GB）
- **刷新频率**：天气 30 分钟，内存 1 分钟
- **点击行为**：刷新数据

---

## 实现步骤

### 1. 创建数据模型 (Models/WeatherData.cs)
- 创建天气 API 响应的数据模型类
- 包含：location、weather、air_quality 等子结构
- 添加内存数据模型

### 2. 创建天气服务 (Services/WeatherService.cs)
- 实现自动 IP 定位获取城市名称
- 调用天气 API：`https://60s.viki.moe/v2/weather`
- 解析 JSON 响应并返回结构化数据
- 缓存天气数据，避免频繁请求

### 3. 创建内存服务 (Services/MemoryService.cs)
- 使用 `System.Diagnostics.PerformanceCounter` 或 `GC` + `Process` 获取内存信息
- 返回已用内存和总内存（GB 格式）

### 4. 创建状态管理服务 (Services/StatusService.cs)
- 统一管理天气和内存数据
- 实现定时刷新逻辑（天气 30 分钟，内存 1 分钟）
- 提供 `INotifyPropertyChanged` 供 UI 绑定

### 5. 创建用户控件 (Controls/StatusControl.xaml/.cs)
- 设计长方形布局，与 Dock 风格一致
- 左侧显示天气图标 + 温度 + 天气状况
- 右侧显示内存占用数值
- 点击时触发数据刷新
- 应用全局样式（背景、透明度、字体等）

### 6. 修改主窗口布局 (MainWindow.xaml)
- 将 StatusControl 放置在 DockItems 左侧
- 使用 StackPanel 水平排列：StatusControl + 分隔 + DockItems
- 保持整体居中对齐

### 7. 更新设置模型 (Models/AppSettings.cs)
- 添加可选的城市覆盖配置（用户可手动指定城市）
- 添加显示/隐藏状态栏选项

### 8. 更新设置窗口 (Windows/SettingsWindow.xaml)
- 添加状态栏相关设置项（可选）

---

## 文件结构

```
QuickDock/
├── Models/
│   └── WeatherData.cs        # 新增：天气数据模型
├── Services/
│   ├── WeatherService.cs     # 新增：天气获取服务
│   ├── MemoryService.cs      # 新增：内存监控服务
│   └── StatusService.cs      # 新增：状态管理服务
├── Controls/
│   └── StatusControl.xaml(.cs) # 新增：状态显示控件
├── MainWindow.xaml           # 修改：添加 StatusControl
└── Models/AppSettings.cs     # 修改：添加相关设置
```

---

## UI 设计要点

### StatusControl 布局示意
```
┌─────────────────────────────────────────┐
│  ☀️  26°C 晴  │  💾 8.2GB / 16GB       │
└─────────────────────────────────────────┘
```

### 样式规范
- 背景色：使用 AppSettings.BackgroundColor
- 透明度：使用 AppSettings.DockOpacity
- 字体：系统默认，与现有 Dock 风格一致
- 圆角：最小化使用，保持简洁
- 无紫色、无高饱和度颜色

---

## 技术细节

### IP 定位方案
使用免费 IP 定位 API 获取城市名称：
- `https://ipapi.co/json/` 或
- `https://ip-api.com/json/`

### 内存获取方案
```csharp
var totalMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
var usedMemory = Process.GetCurrentProcess().WorkingSet64;
// 或使用 PerformanceCounter 获取系统内存
```

### 天气图标映射
根据 `condition_code` 映射到对应的 Unicode 天气图标或图片资源。
