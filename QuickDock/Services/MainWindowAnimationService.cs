using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using QuickDock.Models;

using WpfSize = System.Windows.Size;

namespace QuickDock.Services;

public class MainWindowAnimationService
{
    private readonly Window _window;
    private readonly ConfigService _configService;
    private readonly FrameworkElement _mainGrid;
    private readonly Border _toolsPanel;
    private readonly Border _dockBorder;
    private readonly Func<double> _getDockBorderActualWidth;

    private bool _isHidden = true;
    private bool _isAnimating;
    private bool _hasShownStartupAnimation;
    private bool _isToolsExpanded;
    private double _baseWindowHeight = 70;
    private const int DesiredFrameRate = 120;

    public bool IsHidden => _isHidden;
    public bool IsAnimating => _isAnimating;
    public bool IsToolsExpanded => _isToolsExpanded;

    public event Action? AnimationStarted;
    public event Action? AnimationCompleted;

    public MainWindowAnimationService(
        Window window,
        ConfigService configService,
        FrameworkElement mainGrid,
        Border toolsPanel,
        Border dockBorder,
        Func<double> getDockBorderActualWidth)
    {
        _window = window;
        _configService = configService;
        _mainGrid = mainGrid;
        _toolsPanel = toolsPanel;
        _dockBorder = dockBorder;
        _getDockBorderActualWidth = getDockBorderActualWidth;
    }

    public void ShowStartupAnimation()
    {
        if (_hasShownStartupAnimation) return;
        _hasShownStartupAnimation = true;

        _window.Show();
        SlideIn();

        _ = ShowStartupAnimationAsync();
    }

    private async System.Threading.Tasks.Task ShowStartupAnimationAsync()
    {
        await System.Threading.Tasks.Task.Delay(Math.Max(0, _configService.Settings.StartupPreviewDuration));
        await _window.Dispatcher.InvokeAsync(() =>
        {
            if (!_isAnimating && !_isHidden)
            {
                SlideOut();
            }
        });
    }

    public void SlideIn()
    {
        if (!_isHidden || _isAnimating) return;

        _isAnimating = true;
        AnimationStarted?.Invoke();
        _window.Show();

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
            AnimationCompleted?.Invoke();
        };

        _window.BeginAnimation(Window.TopProperty, animation);
    }

    public void SlideOut()
    {
        if (_isHidden || _isAnimating) return;

        AnimationStarted?.Invoke();
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
            _window.Hide();
        };

        _window.BeginAnimation(Window.TopProperty, animation);
    }

    public void ExpandToolsPanel()
    {
        _isToolsExpanded = true;
        _toolsPanel.Visibility = Visibility.Visible;
        _toolsPanel.BeginAnimation(FrameworkElement.HeightProperty, null);
        _toolsPanel.Height = double.NaN;
        _toolsPanel.UpdateLayout();

        var targetHeight = MeasureToolsPanelHeight();
        _toolsPanel.Height = 0;

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
            _toolsPanel.BeginAnimation(FrameworkElement.HeightProperty, null);
            _toolsPanel.Height = targetHeight;
        };

        _toolsPanel.BeginAnimation(FrameworkElement.HeightProperty, animation);
    }

    public void CollapseToolsPanel()
    {
        _isToolsExpanded = false;

        var targetHeight = _toolsPanel.ActualHeight;
        if (targetHeight <= 0) targetHeight = _baseWindowHeight * _configService.Settings.Scale;

        _toolsPanel.Height = targetHeight;

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
            _toolsPanel.BeginAnimation(FrameworkElement.HeightProperty, null);
            _toolsPanel.Visibility = Visibility.Collapsed;
            _toolsPanel.Height = 0;
        };

        _toolsPanel.BeginAnimation(FrameworkElement.HeightProperty, animation);
    }

    public double GetScaledHeight()
    {
        return _baseWindowHeight * _configService.Settings.Scale;
    }

    public void SetHidden(bool hidden)
    {
        _isHidden = hidden;
    }

    private double MeasureToolsPanelHeight()
    {
        _window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
        var availableWidth = _getDockBorderActualWidth() > 0 ? _getDockBorderActualWidth() : double.PositiveInfinity;
        _toolsPanel.Measure(new WpfSize(availableWidth, double.PositiveInfinity));
        var desiredHeight = _toolsPanel.DesiredSize.Height;

        if (desiredHeight <= 0)
        {
            desiredHeight = _baseWindowHeight * _configService.Settings.Scale;
        }

        return desiredHeight;
    }
}
