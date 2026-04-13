using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QuickDock.Models;
using QuickDock.Services;
using QuickDock.Windows;
using QuickDock.Controls;

using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

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
    private bool _suppressSizeChange;

    public ObservableCollection<DockItem> Items { get; }
    public ObservableCollection<ToolItem> Tools { get; }

    public MainWindow(ConfigService configService)
    {
        InitializeComponent();
        _configService = configService;
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
        await System.Threading.Tasks.Task.Delay(1500);
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
    }

    private void ApplyToolsButtonVisibility()
    {
        ToolsButton.Visibility = _configService.Settings.ToolsEnabled
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ApplyStatusBarVisibility()
    {
        StatusControl.Visibility = _configService.Settings.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ApplyOpacity()
    {
        Opacity = _configService.Settings.DockOpacity;
    }

    private void ApplyBackgroundColor()
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(_configService.Settings.BackgroundColor);
            DockBorder.Background = new SolidColorBrush(color);
            ToolsPanel.Background = new SolidColorBrush(color);
        }
        catch
        {
            DockBorder.Background = new SolidColorBrush(WpfColor.FromRgb(0x1e, 0x1e, 0x1e));
            ToolsPanel.Background = new SolidColorBrush(WpfColor.FromRgb(0x1e, 0x1e, 0x1e));
        }
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
    }

    private double GetScaledHeight()
    {
        return _baseWindowHeight * _configService.Settings.Scale;
    }

    private void PositionWindow()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var scale = _configService.Settings.Scale;
        var scaledHeight = GetScaledHeight();
        Left = (screenWidth - ActualWidth) / 2;
        Top = -scaledHeight;
        _isHidden = true;
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
            Duration = TimeSpan.FromSeconds(0.25),
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
            Duration = TimeSpan.FromSeconds(0.25),
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

        if (System.Windows.Input.Mouse.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            return;

        if (!GetCursorPos(out POINT point))
            return;

        var dpiScale = VisualTreeHelper.GetDpi(this);
        var windowLeft = Left * dpiScale.DpiScaleX;
        var windowRight = (Left + ActualWidth) * dpiScale.DpiScaleX;
        var windowTop = 0;
        var windowBottom = (ActualHeight * _configService.Settings.Scale) * dpiScale.DpiScaleY;

        var isMouseOverWindow = point.X >= windowLeft && point.X <= windowRight &&
                                point.Y >= windowTop && point.Y <= windowBottom + 10;

        if (!isMouseOverWindow)
        {
            SlideOut();
        }
    }

    private void OnDockMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
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
        _suppressSizeChange = true;
        
        var currentWidth = ActualWidth;
        Width = currentWidth;
        
        ToolsPanel.Visibility = Visibility.Visible;
        
        var targetHeight = _baseWindowHeight * _configService.Settings.Scale;
        var animation = new DoubleAnimation
        {
            From = 0,
            To = targetHeight,
            Duration = TimeSpan.FromSeconds(0.15),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };
        
        animation.Completed += (s, e) =>
        {
            ToolsPanel.Height = double.NaN;
            Width = double.NaN;
            _suppressSizeChange = false;
        };
        
        ToolsPanel.BeginAnimation(HeightProperty, animation);
    }

    private void CollapseToolsPanel()
    {
        _isToolsExpanded = false;
        _suppressSizeChange = true;
        
        var currentWidth = ActualWidth;
        Width = currentWidth;
        
        var targetHeight = ToolsPanel.ActualHeight;
        if (targetHeight <= 0) targetHeight = _baseWindowHeight * _configService.Settings.Scale;
        
        ToolsPanel.Height = targetHeight;
        
        var animation = new DoubleAnimation
        {
            From = targetHeight,
            To = 0,
            Duration = TimeSpan.FromSeconds(0.15),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn },
            FillBehavior = FillBehavior.Stop
        };
        
        animation.Completed += (s, e) =>
        {
            ToolsPanel.Visibility = Visibility.Collapsed;
            ToolsPanel.Height = 0;
            Width = double.NaN;
            _suppressSizeChange = false;
        };
        
        ToolsPanel.BeginAnimation(HeightProperty, animation);
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
        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;

        var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
        if (files == null) return;

        foreach (var file in files)
        {
            var ext = System.IO.Path.GetExtension(file).ToLower();
            if (ext != ".exe" && ext != ".lnk") continue;

            var name = System.IO.Path.GetFileNameWithoutExtension(file);
            
            var exists = _configService.Items.Any(i => 
                i.Path.Equals(file, StringComparison.OrdinalIgnoreCase));
            if (exists) continue;

            var item = new DockItem
            {
                Name = name,
                Type = DockItemType.Application,
                Path = file
            };
            _configService.Items.Add(item);
            Items.Add(item);
        }
        _configService.Save();
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
        if (_suppressSizeChange) return;
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - ActualWidth) / 2;
    }
}
