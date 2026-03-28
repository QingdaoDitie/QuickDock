# QuickDock - 快速启动Dock实现计划

## 项目概述
一个C# WPF编写的快速启动Dock，仿VS Code深色风格，顶部中间隐藏，鼠标触碰热区时滑出。

## 技术栈
- **框架**: WPF (.NET 6/7/8)
- **语言**: C#
- **UI风格**: 深色主题，仿VS Code风格

## 核心功能需求
1. 无边框顶部中间悬浮条
2. 平时隐藏在屏幕顶部边缘外
3. 鼠标触碰顶部中间热区时滑出
4. 系统托盘常驻
5. 开机自启动
6. 本地配置文件 (config.json)
7. 图标存储目录 (icons/)
8. 支持类型：应用、文件夹、网页、命令
9. 简单管理界面
10. 默认第一个应用是CMD

---

## 项目结构

```
QuickDock/
├── QuickDock.sln
├── QuickDock/
│   ├── QuickDock.csproj
│   ├── App.xaml(.cs)
│   ├── MainWindow.xaml(.cs)
│   ├── Models/
│   │   └── DockItem.cs
│   ├── Services/
│   │   ├── ConfigService.cs
│   │   └── HotZoneService.cs
│   ├── ViewModels/
│   │   └── MainViewModel.cs
│   ├── Controls/
│   │   └── DockItemControl.xaml(.cs)
│   ├── Windows/
│   │   └── SettingsWindow.xaml(.cs)
│   ├── Resources/
│   │   └── Styles.xaml
│   └── Assets/
│       └── default_icon.ico
├── config.json (运行时生成)
└── icons/ (运行时创建)
```

---

## 实现步骤

### 阶段1: 项目基础搭建
1. **创建WPF项目**
   - 创建QuickDock.sln解决方案
   - 创建QuickDock.csproj (.NET 8)
   - 配置项目为Windows应用

2. **创建数据模型**
   - DockItem.cs: 定义Dock项的数据结构
     - Id, Name, Type, Path, IconPath, Arguments

3. **创建配置服务**
   - ConfigService.cs: 读写config.json
   - 默认配置：包含CMD作为第一个项

### 阶段2: 主窗口与核心UI
4. **创建主窗口**
   - MainWindow.xaml: 无边框、透明背景
   - 位置：屏幕顶部中间
   - 默认状态：Y坐标为负（隐藏在屏幕外）

5. **创建Dock样式**
   - 深色主题样式
   - 仿VS Code配色 (#1e1e1e, #3c3c3c, #007acc)
   - 图标横向排列
   - 悬停效果

6. **创建Dock项控件**
   - DockItemControl.xaml: 单个图标按钮
   - 支持图标显示
   - 点击执行对应操作

### 阶段3: 热区与动画
7. **实现热区检测**
   - HotZoneService.cs: 检测鼠标进入顶部中间区域
   - 热区范围：屏幕宽度中间30%，高度5像素

8. **实现滑出动画**
   - 鼠标进入热区：Dock向下滑出
   - 鼠标离开Dock区域：Dock向上隐藏
   - 平滑动画效果 (Storyboard)

### 阶段4: 功能实现
9. **实现启动功能**
   - 应用启动 (Process.Start)
   - 文件夹打开 (Explorer)
   - 网页打开 (默认浏览器)
   - 命令执行 (CMD)

10. **实现拖放添加**
    - 支持拖放文件/文件夹到Dock
    - 自动创建Dock项

### 阶段5: 托盘与自启动
11. **系统托盘**
    - 使用NotifyIcon (Hardcodet.NotifyIcon.Wpf或自定义)
    - 右键菜单：显示、设置、退出
    - 托盘图标

12. **开机自启动**
    - 注册表方式或启动文件夹快捷方式
    - 设置界面中提供开关

### 阶段6: 管理界面
13. **创建设置窗口**
    - SettingsWindow.xaml
    - 功能：
      - 添加/编辑/删除Dock项
      - 调整顺序
      - 设置热区大小
      - 开机自启动开关

### 阶段7: 完善与测试
14. **完善细节**
    - 图标提取/缓存
    - 错误处理
    - 日志记录

15. **测试与优化**
    - 多显示器支持
    - 性能优化
    - 内存占用优化

---

## 默认配置 (config.json)

```json
{
  "items": [
    {
      "id": "1",
      "name": "CMD",
      "type": "Application",
      "path": "cmd.exe",
      "iconPath": "",
      "arguments": ""
    }
  ],
  "settings": {
    "autoStart": true,
    "hotZoneWidth": 0.3,
    "animationDuration": 200
  }
}
```

---

## UI设计规范

### 颜色方案 (VS Code风格)
- 主背景: #1e1e1e
- 次背景: #2d2d2d
- 悬停背景: #3c3c3c
- 边框: #3c3c3c
- 强调色: #007acc
- 文字: #cccccc
- 悬停文字: #ffffff

### 尺寸
- Dock高度: 60px
- 图标大小: 40x40
- 图标间距: 8px
- 圆角: 8px

---

## 关键技术点

1. **窗口穿透**: 使用 `WindowStyle="None"` + `AllowsTransparency="True"`
2. **顶层显示**: `Topmost="True"`
3. **热区检测**: 使用全局鼠标钩子或定时器检测
4. **动画**: WPF Storyboard + DoubleAnimation
5. **托盘**: Hardycodet.NotifyIcon.Wpf NuGet包
6. **JSON处理**: System.Text.Json

---

## 预计文件数量
- 总计约 15-20 个文件
- 核心代码约 1000-1500 行
