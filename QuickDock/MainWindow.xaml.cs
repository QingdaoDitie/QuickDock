using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using QuickDock.Models;
using QuickDock.Services;
using QuickDock.Windows;
using QuickDock.Controls;

using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfIDataObject = System.Windows.IDataObject;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;

namespace QuickDock;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private readonly ConfigService _configService;
    private readonly LaunchService _launchService;
    private readonly StatusService _statusService;
    private readonly System.Windows.Threading.DispatcherTimer _mouseCheckTimer;
    private bool _isHidden = true;
    private bool _isAnimating;
    private double _baseWindowHeight = 70;
    private bool _hasShownStartupAnimation = false;
    private const int DesiredFrameRate = 120;
    private bool _isToolsExpanded;
    private WpfPoint _dragStartPoint;
    private bool _isInternalDrag;
    private DateTime _autoHideSuppressedUntil = DateTime.MinValue;
    private const string DockItemDragFormat = "QuickDock.DockItem";
    private const string ToolItemDragFormat = "QuickDock.ToolItem";

    public ObservableCollection<DockItem> Items { get; }
    public ObservableCollection<ToolItem> Tools { get; }
    public double ToolIconSize => _configService.Settings.ToolIconSize;
    public bool IsAutoHideSuppressed => _isInternalDrag || DateTime.Now < _autoHideSuppressedUntil;

    public MainWindow(ConfigService configService)
    {
        InitializeComponent();
        _configService = configService;
        _configService.SettingsChanged += OnSettingsChanged;
        _launchService = new LaunchService();
        _statusService = new StatusService(configService);
        
        DockItemControl.SharedConfigService = _configService;
        StatusControl.StatusService = _statusService;
        
        Items = new ObservableCollection<DockItem>(_configService.Items);
        Tools = new ObservableCollection<ToolItem>(
            _configService.Settings.ToolsItems
                .Where(t => t.IsConfirmed)
                .OrderBy(t => t.Order));
        DataContext = this;

        _mouseCheckTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _mouseCheckTimer.Tick += OnMouseCheckTimerTick;

        ApplySettings();
        LoadToolsButtonIcon();
        PositionWindow();
    }
    
    public void ShowStartupAnimation()
    {
        if (_hasShownStartupAnimation) return;
        _hasShownStartupAnimation = true;
        
        Show();
        SlideIn();
        
        _ = ShowStartupAnimationAsync();
    }
    
    private async System.Threading.Tasks.Task ShowStartupAnimationAsync()
    {
        await System.Threading.Tasks.Task.Delay(Math.Max(0, _configService.Settings.StartupPreviewDuration));
        await Dispatcher.InvokeAsync(() =>
        {
            if (!_isAnimating && !_isHidden)
            {
                SlideOut();
            }
        });
    }

    private void ApplySettings()
    {
        ApplyOpacity();
        ApplyBackgroundColor();
        ApplyScale();
        ApplyIconSpacing();
        ApplyStatusBarVisibility();
        ApplyToolsButtonVisibility();
        ApplyToolsIconSpacing();
        ApplyToolButtonSize();
        ToolsButton.ToolTip = Lang.T("Tools.ToolTip");
    }

    private void OnSettingsChanged(string? propertyName)
    {
        Dispatcher.Invoke(() =>
        {
            ApplySettings();

            if (propertyName is nameof(AppSettings.Scale)
                or nameof(AppSettings.IconSize)
                or nameof(AppSettings.ToolIconSize)
                or nameof(AppSettings.IconSpacing)
                or nameof(AppSettings.ShowStatusBar)
                or nameof(AppSettings.ToolsEnabled))
            {
                RecenterWindow();
            }
        });
    }

    private void ApplyToolsButtonVisibility()
    {
        ToolsButton.Visibility = _configService.Settings.ToolsEnabled && Tools.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ApplyStatusBarVisibility()
    {
        var visible = _configService.Settings.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed;
        StatusControl.Visibility = visible;
        StatusBarSeparator.Visibility = visible;
    }

    private void ApplyOpacity()
    {
        Opacity = _configService.Settings.DockOpacity;
    }

    private void ApplyBackgroundColor()
    {
        SolidColorBrush brush;
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(_configService.Settings.BackgroundColor);
            brush = new SolidColorBrush(color);
        }
        catch
        {
            brush = new SolidColorBrush(WpfColor.FromRgb(0x1e, 0x1e, 0x1e));
        }
        DockBorder.Background = brush;
         ToolsPanel.Background = brush;
         StatusControl.RootBorder.Background = brush;
    }

    private void ApplyScale()
    {
        var scale = _configService.Settings.Scale;
        MainGrid.LayoutTransform = new ScaleTransform(scale, scale);
    }

    private void ApplyIconSpacing()
    {
        var spacing = _configService.Settings.IconSpacing;
        var style = new Style(typeof(ContentPresenter));
        style.Setters.Add(new Setter(MarginProperty, new Thickness(spacing / 2, 0, spacing / 2, 0)));
        DockItems.ItemContainerStyle = style;
    }

    private void ApplyToolsIconSpacing()
    {
        var spacing = _configService.Settings.IconSpacing;
        var style = new Style(typeof(ContentPresenter));
        style.Setters.Add(new Setter(MarginProperty, new Thickness(spacing / 2, 0, spacing / 2, 0)));
        ToolsItemsControl.ItemContainerStyle = style;
    }

    private void LoadToolsButtonIcon()
    {
        var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icons", "tools.png");
        if (System.IO.File.Exists(iconPath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                ToolsButtonIcon.Source = bitmap;
                ToolsButtonIcon.Visibility = Visibility.Visible;
                ToolsButtonFallbackText.Visibility = Visibility.Collapsed;
                return;
            }
            catch
            {
            }
        }

        ToolsButtonIcon.Source = null;
        ToolsButtonIcon.Visibility = Visibility.Collapsed;
        ToolsButtonFallbackText.Visibility = Visibility.Visible;
    }

    private void ApplyToolButtonSize()
    {
        var size = _configService.Settings.ToolIconSize;
        ToolsButtonIcon.Width = size;
        ToolsButtonIcon.Height = size;
    }

    private ItemsControl ToolsItemsControl => ToolsItems;

    public void RefreshOpacity()
    {
        ApplyOpacity();
    }

    public void RefreshSettings()
    {
        ApplySettings();
        RefreshItems();
        RefreshTools();
        LoadToolsButtonIcon();
        PositionWindow();
    }

    public void RefreshItems()
    {
        Items.Clear();
        foreach (var item in _configService.Items)
        {
            Items.Add(item);
        }
    }

    public void RefreshTools()
    {
        Tools.Clear();
        foreach (var tool in _configService.Settings.ToolsItems
                     .Where(t => t.IsConfirmed)
                     .OrderBy(t => t.Order))
        {
            Tools.Add(tool);
        }
        
        if (_isToolsExpanded && Tools.Count == 0)
        {
            CollapseToolsPanel();
        }

        ApplyToolsButtonVisibility();
    }

    private double GetScaledHeight()
    {
        return _baseWindowHeight * _configService.Settings.Scale;
    }

    private void PositionWindow()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var scaledHeight = GetScaledHeight();
        Left = (screenWidth - ActualWidth) / 2;
        Top = -scaledHeight;
        _isHidden = true;
    }

    private void RecenterWindow()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - ActualWidth) / 2;
    }

    public void SlideIn()
    {
        if (!_isHidden || _isAnimating) return;
        
        _isAnimating = true;
        Show();
        
        var scaledHeight = GetScaledHeight();
        var animation = new DoubleAnimation
        {
            From = -scaledHeight,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(_configService.Settings.DockShowAnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.HoldEnd
        };
        
        Timeline.SetDesiredFrameRate(animation, DesiredFrameRate);
        
        animation.Completed += (s, e) =>
        {
            _isHidden = false;
            _isAnimating = false;
            _mouseCheckTimer.Start();
        };
        
        BeginAnimation(TopProperty, animation);
    }

    public void SlideOut()
    {
        if (_isHidden || _isAnimating) return;

        _mouseCheckTimer.Stop();
        _isAnimating = true;

        if (_isToolsExpanded)
        {
            CollapseToolsPanel();
        }
        
        var scaledHeight = GetScaledHeight();
        var animation = new DoubleAnimation
        {
            From = 0,
            To = -scaledHeight,
            Duration = TimeSpan.FromMilliseconds(_configService.Settings.DockHideAnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            FillBehavior = FillBehavior.HoldEnd
        };
        
        Timeline.SetDesiredFrameRate(animation, DesiredFrameRate);
        
        animation.Completed += (s, e) =>
        {
            _isHidden = true;
            _isAnimating = false;
            Hide();
        };
        
        BeginAnimation(TopProperty, animation);
    }

    private void OnMouseCheckTimerTick(object? sender, EventArgs e)
    {
        if (_isHidden || _isAnimating)
        {
            _mouseCheckTimer.Stop();
            return;
        }

        if (_isInternalDrag)
        {
            return;
        }

        if (IsAutoHideSuppressed)
        {
            return;
        }

        if (System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            return;

        if (!GetCursorPos(out POINT point))
            return;

        var dpiScale = VisualTreeHelper.GetDpi(this);
        var windowLeft = Left * dpiScale.DpiScaleX;
        var windowRight = (Left + ActualWidth) * dpiScale.DpiScaleX;
        var windowTop = 0;
        var windowBottom = (ActualHeight * _configService.Settings.Scale) * dpiScale.DpiScaleY;

        var tolerance = _configService.Settings.AutoHideTolerance;
        var isMouseOverWindow = point.X >= windowLeft && point.X <= windowRight &&
                                point.Y >= windowTop && point.Y <= windowBottom + tolerance;

        if (!isMouseOverWindow)
        {
            SlideOut();
        }
    }

    private void OnDockMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (IsAutoHideSuppressed)
        {
            return;
        }

        if (!_isAnimating && !_isHidden)
        {
            var position = e.GetPosition(MainGrid);
            if (position.Y > MainGrid.ActualHeight || position.X < 0 || position.X > MainGrid.ActualWidth)
            {
                SlideOut();
            }
        }
    }

    private void OnToolsButtonClick(object sender, RoutedEventArgs e)
    {
        if (!_configService.Settings.ToolsEnabled) return;
        
        if (Tools.Count == 0) return;

        if (_isToolsExpanded)
        {
            CollapseToolsPanel();
        }
        else
        {
            ExpandToolsPanel();
        }
    }

    private void ExpandToolsPanel()
    {
        _isToolsExpanded = true;
        ToolsPanel.Visibility = Visibility.Visible;
        ToolsPanel.BeginAnimation(HeightProperty, null);
        ToolsPanel.Height = double.NaN;
        ToolsPanel.UpdateLayout();

        var targetHeight = MeasureToolsPanelHeight();
        ToolsPanel.Height = 0;

        var animation = new DoubleAnimation
        {
            From = 0,
            To = targetHeight,
            Duration = TimeSpan.FromMilliseconds(_configService.Settings.ToolsExpandAnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.HoldEnd
        };
        
        animation.Completed += (s, e) =>
        {
            ToolsPanel.BeginAnimation(HeightProperty, null);
            ToolsPanel.Height = targetHeight;
        };
        
        ToolsPanel.BeginAnimation(HeightProperty, animation);
    }

    private void CollapseToolsPanel()
    {
        _isToolsExpanded = false;
        
        var targetHeight = ToolsPanel.ActualHeight;
        if (targetHeight <= 0) targetHeight = _baseWindowHeight * _configService.Settings.Scale;
        
        ToolsPanel.Height = targetHeight;
        
        var animation = new DoubleAnimation
        {
            From = targetHeight,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(_configService.Settings.ToolsCollapseAnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            FillBehavior = FillBehavior.HoldEnd
        };
        
        animation.Completed += (s, e) =>
        {
            ToolsPanel.BeginAnimation(HeightProperty, null);
            ToolsPanel.Visibility = Visibility.Collapsed;
            ToolsPanel.Height = 0;
        };
        
        ToolsPanel.BeginAnimation(HeightProperty, animation);
    }

    private double MeasureToolsPanelHeight()
    {
        Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
        var availableWidth = DockBorder.ActualWidth > 0 ? DockBorder.ActualWidth : double.PositiveInfinity;
        ToolsPanel.Measure(new WpfSize(availableWidth, double.PositiveInfinity));
        var desiredHeight = ToolsPanel.DesiredSize.Height;

        if (desiredHeight <= 0)
        {
            desiredHeight = _baseWindowHeight * _configService.Settings.Scale;
        }

        return desiredHeight;
    }

    public void LaunchItem(DockItem item)
    {
        _launchService.Launch(item);
        SlideOut();
    }

    public void LaunchToolItem(ToolItem tool)
    {
        _launchService.LaunchApplication(tool.ExePath);
        SlideOut();
    }

    private void OnWindowDragEnter(object sender, System.Windows.DragEventArgs e)
    {
        HandleDragEvent(e);
    }

    private void OnWindowDragOver(object sender, System.Windows.DragEventArgs e)
    {
        HandleDragEvent(e);
    }

    private void HandleDragEvent(System.Windows.DragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files != null)
            {
                foreach (var file in files)
                {
                    var ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext == ".exe" || ext == ".lnk")
                    {
                        e.Effects = System.Windows.DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }
        }
        e.Effects = System.Windows.DragDropEffects.None;
        e.Handled = true;
    }

    private void OnWindowDrop(object sender, System.Windows.DragEventArgs e)
    {
        if (TryHandleInternalDrop(e))
        {
            return;
        }

        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;

        var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
        if (files == null) return;

        foreach (var file in files)
        {
            var ext = System.IO.Path.GetExtension(file).ToLower();
            if (ext != ".exe" && ext != ".lnk") continue;

            string targetPath = file;
            string name = System.IO.Path.GetFileNameWithoutExtension(file);

            if (ext == ".lnk")
            {
                var resolved = ResolveShortcut(file);
                if (resolved != null)
                {
                    targetPath = resolved;
                    name = System.IO.Path.GetFileNameWithoutExtension(resolved);
                }
            }

            var exists = _configService.Items.Any(i => 
                i.Path.Equals(targetPath, StringComparison.OrdinalIgnoreCase));
            if (exists) continue;

            var item = new DockItem
            {
                Name = name,
                Type = DockItemType.Application,
                Path = targetPath
            };
            _configService.Items.Add(item);
            Items.Add(item);
        }
        _configService.Save();
    }

    private bool TryHandleInternalDrop(WpfDragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DockItemDragFormat) && !e.Data.GetDataPresent(ToolItemDragFormat))
        {
            return false;
        }

        e.Effects = WpfDragDropEffects.Move;
        e.Handled = true;
        return true;
    }

    private static string? ResolveShortcut(string shortcutPath)
    {
        try
        {
            if (!System.IO.File.Exists(shortcutPath)) return null;
            
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return null;
            
            var shellInstance = Activator.CreateInstance(shellType);
            if (shellInstance == null) return null;
            
            var shortcut = shellType.InvokeMember("CreateShortcut", 
                System.Reflection.BindingFlags.InvokeMethod, 
                null, shellInstance, new object[] { shortcutPath });
            
            if (shortcut == null) return null;
            
            var targetPath = (string?)shellType.InvokeMember("TargetPath",
                System.Reflection.BindingFlags.GetProperty,
                null, shortcut, null);
            
            Marshal.ReleaseComObject(shortcut);
            Marshal.ReleaseComObject(shellInstance);
            
            return targetPath;
        }
        catch
        {
            return null;
        }
    }

    private void OnWindowPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(this);
    }

    private void OnWindowPreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isAnimating || _isInternalDrag)
        {
            return;
        }

        var currentPosition = e.GetPosition(this);
        if (Math.Abs(currentPosition.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var dockControl = FindAncestor<DockItemControl>(e.OriginalSource as DependencyObject);
        if (dockControl?.DataContext is DockItem dockItem)
        {
            StartInternalDrag(new System.Windows.DataObject(DockItemDragFormat, dockItem));
            return;
        }

        if (dockControl?.DataContext is ToolItem toolItem)
        {
            StartInternalDrag(new System.Windows.DataObject(ToolItemDragFormat, toolItem));
        }
    }

    private void StartInternalDrag(WpfIDataObject data)
    {
        try
        {
            _isInternalDrag = true;
            _autoHideSuppressedUntil = DateTime.MaxValue;
            _mouseCheckTimer.Stop();
            DragDrop.DoDragDrop(this, data, WpfDragDropEffects.Move);
        }
        finally
        {
            _isInternalDrag = false;
            _autoHideSuppressedUntil = DateTime.Now.AddMilliseconds(350);
            if (!_isHidden && !_isAnimating)
            {
                _mouseCheckTimer.Start();
            }
        }
    }

    private void OnDockItemsDragOver(object sender, WpfDragEventArgs e)
    {
        if (e.Data.GetDataPresent(DockItemDragFormat))
        {
            e.Effects = WpfDragDropEffects.Move;
            e.Handled = true;
            return;
        }

        HandleDragEvent(e);
    }

    private void OnDockItemsDrop(object sender, WpfDragEventArgs e)
    {
        if (e.Data.GetData(DockItemDragFormat) is not DockItem draggedItem)
        {
            OnWindowDrop(sender, e);
            return;
        }

        var targetIndex = GetDropIndex(DockItems, e.GetPosition(DockItems), Items.Count);
        MoveDockItem(draggedItem, targetIndex);
        e.Handled = true;
    }

    private void OnToolsItemsDragOver(object sender, WpfDragEventArgs e)
    {
        if (e.Data.GetDataPresent(ToolItemDragFormat))
        {
            e.Effects = WpfDragDropEffects.Move;
        }
        else
        {
            e.Effects = WpfDragDropEffects.None;
        }

        e.Handled = true;
    }

    private void OnToolsItemsDrop(object sender, WpfDragEventArgs e)
    {
        if (e.Data.GetData(ToolItemDragFormat) is not ToolItem draggedItem)
        {
            return;
        }

        var targetIndex = GetDropIndex(ToolsItems, e.GetPosition(ToolsItems), Tools.Count);
        MoveToolItem(draggedItem, targetIndex);
        e.Handled = true;
    }

    private int GetDropIndex(ItemsControl itemsControl, WpfPoint position, int itemCount)
    {
        if (itemCount == 0)
        {
            return 0;
        }

        for (int i = 0; i < itemCount; i++)
        {
            if (itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
            {
                continue;
            }

            var topLeft = container.TranslatePoint(new WpfPoint(0, 0), itemsControl);
            var midpoint = topLeft.X + container.ActualWidth / 2;
            if (position.X < midpoint)
            {
                return i;
            }
        }

        return itemCount;
    }

    private void MoveDockItem(DockItem draggedItem, int targetIndex)
    {
        var sourceIndex = Items.IndexOf(draggedItem);
        if (sourceIndex < 0)
        {
            return;
        }

        if (targetIndex > sourceIndex)
        {
            targetIndex--;
        }

        targetIndex = Math.Clamp(targetIndex, 0, Math.Max(0, Items.Count - 1));
        if (sourceIndex == targetIndex)
        {
            return;
        }

        Items.Move(sourceIndex, targetIndex);
        _configService.Items.Clear();
        foreach (var item in Items)
        {
            _configService.Items.Add(item);
        }
        _configService.Save();
    }

    private void MoveToolItem(ToolItem draggedItem, int targetIndex)
    {
        var sourceIndex = Tools.IndexOf(draggedItem);
        if (sourceIndex < 0)
        {
            return;
        }

        if (targetIndex > sourceIndex)
        {
            targetIndex--;
        }

        targetIndex = Math.Clamp(targetIndex, 0, Math.Max(0, Tools.Count - 1));
        if (sourceIndex == targetIndex)
        {
            return;
        }

        Tools.Move(sourceIndex, targetIndex);
        for (int i = 0; i < Tools.Count; i++)
        {
            Tools[i].Order = i;
        }

        var pendingTools = _configService.Settings.ToolsItems
            .Where(t => !t.IsConfirmed)
            .ToList();
        _configService.Settings.ToolsItems = Tools.Concat(pendingTools).ToList();
        _configService.Save();
        RefreshTools();
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    public void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_configService, new AutoStartService());
        settingsWindow.Owner = this;
        if (settingsWindow.ShowDialog() == true)
        {
            RefreshSettings();
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        RecenterWindow();
    }

    protected override void OnClosed(EventArgs e)
    {
        _configService.SettingsChanged -= OnSettingsChanged;
        _statusService.Dispose();
        base.OnClosed(e);
    }
}
