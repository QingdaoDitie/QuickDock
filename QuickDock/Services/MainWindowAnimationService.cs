using System.Windows;
using System.Windows.Media.Animation;

namespace QuickDock.Services;

public class MainWindowAnimationService
{
    private readonly Window _window;
    private readonly ConfigService _configService;
    private readonly FrameworkElement _mainGrid;
    private readonly FrameworkElement _toolsPanelViewport;
    private readonly FrameworkElement _toolsPanel;

    private bool _isHidden = true;
    private bool _isAnimating;
    private bool _hasShownStartupAnimation;
    private bool _isToolsExpanded;
    private DoubleAnimation? _toolsPanelAnimation;
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
        FrameworkElement toolsPanelViewport,
        FrameworkElement toolsPanel)
    {
        _window = window;
        _configService = configService;
        _mainGrid = mainGrid;
        _toolsPanelViewport = toolsPanelViewport;
        _toolsPanel = toolsPanel;
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
            _isToolsExpanded = false;
            SetToolsPanelRestingState();
        };

        _window.BeginAnimation(Window.TopProperty, animation);
    }

    public void HideImmediately()
    {
        AnimationStarted?.Invoke();
        _window.BeginAnimation(Window.TopProperty, null);
        _window.Top = -GetScaledHeight();
        _isHidden = true;
        _isAnimating = false;
        _window.Hide();
        _isToolsExpanded = false;
        SetToolsPanelRestingState();
    }

    public void ExpandToolsPanel()
    {
        _isToolsExpanded = true;
        _toolsPanelViewport.Visibility = Visibility.Visible;
        _toolsPanelViewport.UpdateLayout();
        var targetHeight = _toolsPanel.ActualHeight;
        AnimateToolsPanelHeight(targetHeight, _configService.Settings.ToolsExpandAnimationDuration, EasingMode.EaseOut);
    }

    public void CollapseToolsPanel()
    {
        _isToolsExpanded = false;
        AnimateToolsPanelHeight(0, _configService.Settings.ToolsCollapseAnimationDuration, EasingMode.EaseIn);
    }

    private void AnimateToolsPanelHeight(double targetHeight, int duration, EasingMode easingMode)
    {
        var currentHeight = _toolsPanelViewport.Height;
        var animation = new DoubleAnimation
        {
            From = double.IsNaN(currentHeight) ? _toolsPanelViewport.ActualHeight : currentHeight,
            To = targetHeight,
            Duration = TimeSpan.FromMilliseconds(duration),
            EasingFunction = new CubicEase { EasingMode = easingMode }
        };

        Timeline.SetDesiredFrameRate(animation, DesiredFrameRate);
        animation.Completed += (_, _) =>
        {
            if (_toolsPanelAnimation == animation)
            {
                SetToolsPanelRestingState();
            }
        };

        _toolsPanelAnimation = animation;
        _toolsPanelViewport.BeginAnimation(FrameworkElement.HeightProperty, animation);
    }

    private void SetToolsPanelRestingState()
    {
        _toolsPanelAnimation = null;
        _toolsPanelViewport.Height = _isToolsExpanded ? double.NaN : 0;
        _toolsPanelViewport.Visibility = _isToolsExpanded ? Visibility.Visible : Visibility.Collapsed;
        _toolsPanelViewport.BeginAnimation(FrameworkElement.HeightProperty, null);
    }

    public double GetScaledHeight()
    {
        return Math.Max(_baseWindowHeight * _configService.Settings.Scale, _mainGrid.ActualHeight);
    }

    public void SetHidden(bool hidden)
    {
        _isHidden = hidden;
    }

}
