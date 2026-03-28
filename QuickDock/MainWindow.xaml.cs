using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QuickDock.Models;
using QuickDock.Services;
using QuickDock.Windows;
using QuickDock.Controls;

namespace QuickDock;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly LaunchService _launchService;
    private readonly StatusService _statusService;
    private bool _isHidden = true;
    private bool _isAnimating;
    private double _baseWindowHeight = 70;
    private bool _hasShownStartupAnimation = false;
    private const int DesiredFrameRate = 120;

    public ObservableCollection<DockItem> Items { get; }

    public MainWindow(ConfigService configService)
    {
        InitializeComponent();
        _configService = configService;
        _launchService = new LaunchService();
        _statusService = new StatusService(configService);
        
        DockItemControl.SharedConfigService = _configService;
        StatusControl.StatusService = _statusService;
        
        Items = new ObservableCollection<DockItem>(_configService.Items);
        DataContext = this;

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
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_configService.Settings.BackgroundColor);
            DockBorder.Background = new SolidColorBrush(color);
        }
        catch
        {
            DockBorder.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1e, 0x1e, 0x1e));
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

    public void RefreshOpacity()
    {
        ApplyOpacity();
    }

    public void RefreshSettings()
    {
        ApplySettings();
        RefreshItems();
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
        };
        
        BeginAnimation(TopProperty, animation);
    }

    public void SlideOut()
    {
        if (_isHidden || _isAnimating) return;

        _isAnimating = true;
        
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

    private void OnDockMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isAnimating && !_isHidden)
        {
            SlideOut();
        }
    }

    public void LaunchItem(DockItem item)
    {
        _launchService.Launch(item);
        SlideOut();
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
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - ActualWidth) / 2;
    }
}
