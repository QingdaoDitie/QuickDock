using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using QuickDock.Models;
using QuickDock.Services;
using QuickDock.Windows;
using QuickDock.Controls;
using QuickDock.WidgetContracts;
using QuickDock.Widgets;

using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace QuickDock;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly LaunchService _launchService;
    private readonly WidgetHost _widgetHost;
    private readonly PopupPolicy _popupPolicy;

    private readonly MainWindowAnimationService _animationService;
    private readonly DragDropHandler _dragDropHandler;
    private readonly SettingsApplier _settingsApplier;
    private readonly AutoHideController _autoHideController;
    private readonly Queue<WidgetNotification> _weatherNotificationQueue = new();
    private bool _isShowingWidgetNotification;
    private WidgetNotification? _currentWidgetNotification;
    private CancellationTokenSource? _weatherNotificationDelayCts;

    public ObservableCollection<DockItem> Items { get; }
    public ObservableCollection<ToolItem> Tools { get; }
    public double ToolIconSize => _configService.Settings.ToolIconSize;
    public bool IsAutoHideSuppressed => _dragDropHandler.IsAutoHideSuppressed || _isShowingWidgetNotification;

    internal MainWindow(ConfigService configService, PopupPolicy popupPolicy)
    {
        InitializeComponent();
        _configService = configService;
        _popupPolicy = popupPolicy;
        _configService.SettingsChanged += OnSettingsChanged;
        _launchService = new LaunchService();
        _widgetHost = new WidgetHost(StatusControl, configService);
        _widgetHost.NotificationRaised += OnWidgetNotificationRaised;
        _widgetHost.NotificationsCleared += ClearWidgetNotification;

        DockItemControl.SharedConfigService = _configService;

        Items = new ObservableCollection<DockItem>(_configService.Items.Where(i => i.Enabled));
        Tools = new ObservableCollection<ToolItem>(
            _configService.Settings.ToolsItems
                .Where(t => t.IsConfirmed && t.Enabled)
                .OrderBy(t => t.Order));
        DataContext = this;

        _animationService = new MainWindowAnimationService(
            this,
            configService,
            MainGrid,
            ToolsPanelViewport,
            ToolsPanel);

        _animationService.AnimationStarted += () =>
        {
            _autoHideController?.Stop();
        };

        _animationService.AnimationCompleted += () =>
        {
            if (!_animationService.IsHidden)
            {
                _autoHideController?.Start();
            }
        };

        _dragDropHandler = new DragDropHandler(
            this,
            configService,
            Items,
            Tools,
            DockItems,
            ToolsItems,
            () => _animationService.IsAnimating,
            () => _animationService.IsHidden,
            RefreshTools);

        _dragDropHandler.AutoHideSuppressionChanged += isSuppressed =>
        {
            if (isSuppressed)
            {
                _autoHideController?.Stop();
            }
            else if (!_animationService.IsHidden && !_animationService.IsAnimating)
            {
                _autoHideController?.Start();
            }
        };

        _settingsApplier = new SettingsApplier(
            this,
            configService,
            MainGrid,
            DockBorder,
            ToolsPanel,
            StatusControl,
            StatusBarSeparator,
            ToolsButton,
            DockItems,
            ToolsItems,
            ToolsButtonIcon,
            ToolsButtonFallbackText,
            () => Tools.Count);

        _autoHideController = new AutoHideController(
            this,
            configService,
            MainGrid,
            () => _animationService.IsHidden,
            () => _animationService.IsAnimating,
            () => _dragDropHandler.IsAutoHideSuppressed || _isShowingWidgetNotification,
            () => _animationService.SlideOut());

        _settingsApplier.ApplyAll();
        _settingsApplier.LoadToolsButtonIcon();
        _widgetHost.ApplySlots();
        PositionWindow();
    }

    public void ShowStartupAnimation()
    {
        if (!_popupPolicy.IsSuppressed)
        {
            _animationService.ShowStartupAnimation();
        }
    }

    public void SlideIn()
    {
        _animationService.SlideIn();
    }

    public void SlideOut()
    {
        _animationService.SlideOut();
    }

    public void ShowLatestWidgetNotificationOnDockTouch()
    {
        Dispatcher.Invoke(() =>
        {
            var latest = _widgetHost.GetLatestNotification();
            if (latest != null)
            {
                EnqueueWidgetNotification(latest);
            }
        });
    }

    public void RefreshOpacity()
    {
        _settingsApplier.ApplyOpacity();
    }

    public void RefreshSettings()
    {
        _settingsApplier.ApplyAll();
        RefreshItems();
        RefreshTools();
        _settingsApplier.LoadToolsButtonIcon();
        PositionWindow();
    }

    public void RefreshItems()
    {
        Items.Clear();
        foreach (var item in _configService.Items.Where(i => i.Enabled))
        {
            Items.Add(item);
        }
    }

    public void RefreshTools()
    {
        Tools.Clear();
        foreach (var tool in _configService.Settings.ToolsItems
                     .Where(t => t.IsConfirmed && t.Enabled)
                     .OrderBy(t => t.Order))
        {
            Tools.Add(tool);
        }

        if (_animationService.IsToolsExpanded && Tools.Count == 0)
        {
            _animationService.CollapseToolsPanel();
        }

        _settingsApplier.ApplyToolsButtonVisibility();
    }

    public void LaunchItem(DockItem item)
    {
        _launchService.Launch(item);
        _animationService.SlideOut();
    }

    public void LaunchToolItem(ToolItem tool)
    {
        _launchService.LaunchApplication(tool.ExePath);
        _animationService.SlideOut();
    }

    internal SettingsWindow CreateSettingsWindow(AutoStartService autoStartService) =>
        new(_configService, autoStartService, _widgetHost);

    public void OpenSettings()
    {
        var settingsWindow = CreateSettingsWindow(new AutoStartService());
        settingsWindow.Owner = this;
        if (settingsWindow.ShowDialog() == true)
        {
            RefreshSettings();
        }
    }

    private void OnSettingsChanged(string? propertyName)
    {
        Dispatcher.Invoke(() =>
        {
            if (propertyName is nameof(AppSettings.Widgets) or nameof(AppSettings.ShowStatusBar))
                _widgetHost.ApplySlots();
            if (propertyName == nameof(AppSettings.Language))
                _widgetHost.UpdateLanguage();

            _settingsApplier.ApplyAll();

            if (propertyName is nameof(AppSettings.Scale)
                or nameof(AppSettings.IconSize)
                or nameof(AppSettings.ToolIconSize)
                or nameof(AppSettings.IconSpacing)
                or nameof(AppSettings.Widgets)
                or nameof(AppSettings.ShowStatusBar)
                or nameof(AppSettings.ToolsEnabled))
            {
                RecenterWindow();
            }
        });
    }

    private void OnWidgetNotificationRaised(WidgetNotification notification)
    {
        Dispatcher.Invoke(() =>
        {
            EnqueueWidgetNotification(notification);
        });
    }

    private void EnqueueWidgetNotification(WidgetNotification notification)
    {
        if (_popupPolicy.IsSuppressed || _currentWidgetNotification?.Key == notification.Key ||
            _weatherNotificationQueue.Any(item => item.Key == notification.Key))
        {
            return;
        }

        _weatherNotificationQueue.Enqueue(notification);
        if (!_isShowingWidgetNotification)
        {
            _ = ShowNextWidgetNotificationAsync();
        }
    }

    private void ClearWidgetNotification()
    {
        _weatherNotificationQueue.Clear();
        _currentWidgetNotification = null;
        _weatherNotificationDelayCts?.Cancel();
        WidgetNotificationPanel.Visibility = Visibility.Collapsed;
    }

    internal void DismissPopups()
    {
        ClearWidgetNotification();
        if (IsVisible)
        {
            _animationService.HideImmediately();
        }
    }

    private async Task ShowNextWidgetNotificationAsync()
    {
        _isShowingWidgetNotification = true;
        while (_weatherNotificationQueue.Count > 0)
        {
            if (_popupPolicy.IsSuppressed)
            {
                DismissPopups();
                break;
            }

            var notification = _weatherNotificationQueue.Dequeue();
            _currentWidgetNotification = notification;
            var wasHidden = _animationService.IsHidden;
            _autoHideController.Stop();

            if (wasHidden)
            {
                _animationService.SlideIn();
                await Task.Delay(_configService.Settings.DockShowAnimationDuration + 50);
            }

            if (_currentWidgetNotification == null)
            {
                continue;
            }

            if (_popupPolicy.IsSuppressed)
            {
                DismissPopups();
                continue;
            }

            ApplyWidgetNotification(notification);
            await AnimateWidgetNotificationAsync(notification, show: true);

            if (_currentWidgetNotification == null)
            {
                continue;
            }

            _weatherNotificationDelayCts?.Dispose();
            _weatherNotificationDelayCts = new CancellationTokenSource();
            try
            {
                await Task.Delay(
                    TimeSpan.FromMilliseconds(notification.DisplayMilliseconds),
                    _weatherNotificationDelayCts.Token);
            }
            catch (OperationCanceledException)
            {
            }

            if (WidgetNotificationPanel.Visibility == Visibility.Visible)
            {
                await AnimateWidgetNotificationAsync(notification, show: false);
            }

            if (_currentWidgetNotification != null && wasHidden && !_animationService.IsHidden)
            {
                await Dispatcher.InvokeAsync(
                    () => { },
                    System.Windows.Threading.DispatcherPriority.Loaded);
                if (_currentWidgetNotification != null)
                {
                    _animationService.SlideOut();
                    await Task.Delay(_configService.Settings.DockHideAnimationDuration + 50);
                }
            }

            _currentWidgetNotification = null;
        }

        _weatherNotificationDelayCts?.Dispose();
        _weatherNotificationDelayCts = null;
        _isShowingWidgetNotification = false;
        if (!_animationService.IsHidden)
        {
            _autoHideController.Start();
        }
    }

    private Task AnimateWidgetNotificationAsync(WidgetNotification notification, bool show)
    {
        var completion = new TaskCompletionSource();
        if (show)
        {
            WidgetNotificationIcon.Text = notification.Icon;
            WidgetNotificationTitle.Text = notification.Title;
            WidgetNotificationMessage.Text = notification.Message;
            WidgetNotificationPanel.Visibility = Visibility.Visible;
            WidgetNotificationPanel.BeginAnimation(HeightProperty, null);
            WidgetNotificationPanel.Height = double.NaN;
            WidgetNotificationPanel.Measure(new System.Windows.Size(WidgetNotificationPanel.Width, double.PositiveInfinity));
            var margin = WidgetNotificationPanel.Margin;
            var targetHeight = Math.Max(0, WidgetNotificationPanel.DesiredSize.Height - margin.Top - margin.Bottom);
            WidgetNotificationPanel.Height = 0;

            var heightAnimation = new DoubleAnimation(
                0,
                targetHeight,
                TimeSpan.FromMilliseconds(notification.ShowMilliseconds));
            var opacityAnimation = new DoubleAnimation(
                0,
                1,
                TimeSpan.FromMilliseconds(Math.Min(140, notification.ShowMilliseconds)));
            heightAnimation.Completed += (_, _) =>
            {
                WidgetNotificationPanel.Height = targetHeight;
                WidgetNotificationPanel.BeginAnimation(HeightProperty, null);
                completion.SetResult();
            };
            WidgetNotificationPanel.BeginAnimation(HeightProperty, heightAnimation);
            WidgetNotificationPanel.BeginAnimation(OpacityProperty, opacityAnimation);
        }
        else
        {
            var heightAnimation = new DoubleAnimation(
                WidgetNotificationPanel.ActualHeight,
                0,
                TimeSpan.FromMilliseconds(notification.HideMilliseconds));
            var opacityAnimation = new DoubleAnimation(
                1,
                0,
                TimeSpan.FromMilliseconds(Math.Min(100, notification.HideMilliseconds)));
            heightAnimation.Completed += (_, _) =>
            {
                WidgetNotificationPanel.Height = 0;
                WidgetNotificationPanel.BeginAnimation(HeightProperty, null);
                WidgetNotificationPanel.Visibility = Visibility.Collapsed;
                completion.SetResult();
            };
            WidgetNotificationPanel.BeginAnimation(HeightProperty, heightAnimation);
            WidgetNotificationPanel.BeginAnimation(OpacityProperty, opacityAnimation);
        }

        return completion.Task;
    }

    private void ApplyWidgetNotification(WidgetNotification notification)
    {
        WidgetNotificationIcon.Text = notification.Icon;
        WidgetNotificationTitle.Text = notification.Title;
        WidgetNotificationMessage.Text = notification.Message;
        WidgetNotificationIcon.Visibility = Visibility.Visible;
        WidgetNotificationTitle.Visibility = Visibility.Visible;
        WidgetNotificationMessage.Visibility = Visibility.Visible;
        WidgetNotificationMuteButton.Content = Lang.CurrentLanguage == QuickDock.Services.Language.Chinese
            ? "休息一会"
            : "Rest until refresh";
        WidgetNotificationDetails.Content = notification.CreateDetails?.Invoke();
    }

    private void OnWidgetNotificationMuteClick(object sender, RoutedEventArgs e)
    {
        if (_currentWidgetNotification == null)
        {
            return;
        }

        _widgetHost.MuteUntilRefresh(_currentWidgetNotification.Group);
        _weatherNotificationQueue.Clear();
        _weatherNotificationDelayCts?.Cancel();
        e.Handled = true;
    }

    private void PositionWindow()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        var scaledHeight = _animationService.GetScaledHeight();
        Left = (screenWidth - ActualWidth) / 2;
        Top = -scaledHeight;
        _animationService.SetHidden(true);
    }

    private void RecenterWindow()
    {
        var screenWidth = SystemParameters.PrimaryScreenWidth;
        Left = (screenWidth - ActualWidth) / 2;
    }

    private void OnDockMouseLeave(object sender, WpfMouseEventArgs e)
    {
        _autoHideController.OnMouseLeave(e);
    }

    private void OnToolsButtonClick(object sender, RoutedEventArgs e)
    {
        if (!_configService.Settings.ToolsEnabled) return;
        if (Tools.Count == 0) return;

        if (_animationService.IsToolsExpanded)
        {
            _animationService.CollapseToolsPanel();
        }
        else
        {
            _animationService.ExpandToolsPanel();
        }
    }

    private void OnWindowDragEnter(object sender, WpfDragEventArgs e)
    {
        _dragDropHandler.OnDragEnter(e);
    }

    private void OnWindowDragOver(object sender, WpfDragEventArgs e)
    {
        _dragDropHandler.OnDragOver(e);
    }

    private void OnWindowDrop(object sender, WpfDragEventArgs e)
    {
        _dragDropHandler.OnDrop(e);
    }

    private void OnWindowPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragDropHandler.OnPreviewMouseLeftButtonDown(e);
    }

    private void OnWindowPreviewMouseMove(object sender, WpfMouseEventArgs e)
    {
        _dragDropHandler.OnPreviewMouseMove(e);
    }

    private void OnDockItemsDragOver(object sender, WpfDragEventArgs e)
    {
        _dragDropHandler.OnDockItemsDragOver(e);
    }

    private void OnDockItemsDrop(object sender, WpfDragEventArgs e)
    {
        _dragDropHandler.OnDockItemsDrop(e);
    }

    private void OnToolsItemsDragOver(object sender, WpfDragEventArgs e)
    {
        _dragDropHandler.OnToolsItemsDragOver(e);
    }

    private void OnToolsItemsDrop(object sender, WpfDragEventArgs e)
    {
        _dragDropHandler.OnToolsItemsDrop(e);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        RecenterWindow();
    }

    protected override void OnClosed(EventArgs e)
    {
        _configService.SettingsChanged -= OnSettingsChanged;
        _widgetHost.NotificationRaised -= OnWidgetNotificationRaised;
        _widgetHost.NotificationsCleared -= ClearWidgetNotification;
        _weatherNotificationDelayCts?.Cancel();
        _weatherNotificationDelayCts?.Dispose();
        _widgetHost.Dispose();
        _autoHideController.Stop();
        base.OnClosed(e);
    }
}
