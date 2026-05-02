using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickDock.Models;
using QuickDock.Services;
using QuickDock.Windows;
using QuickDock.Controls;

using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace QuickDock;

public partial class MainWindow : Window
{
    private readonly ConfigService _configService;
    private readonly LaunchService _launchService;
    private readonly StatusService _statusService;

    private readonly MainWindowAnimationService _animationService;
    private readonly DragDropHandler _dragDropHandler;
    private readonly SettingsApplier _settingsApplier;
    private readonly AutoHideController _autoHideController;

    public ObservableCollection<DockItem> Items { get; }
    public ObservableCollection<ToolItem> Tools { get; }
    public double ToolIconSize => _configService.Settings.ToolIconSize;
    public bool IsAutoHideSuppressed => _dragDropHandler.IsAutoHideSuppressed;

    public MainWindow(ConfigService configService)
    {
        InitializeComponent();
        _configService = configService;
        _configService.SettingsChanged += OnSettingsChanged;
        _launchService = new LaunchService();
        _statusService = new StatusService(configService);

        DockItemControl.SharedConfigService = _configService;
        StatusControl.StatusService = _statusService;

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
            ToolsPanel,
            DockBorder,
            () => DockBorder.ActualWidth);

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
            () => _dragDropHandler.IsAutoHideSuppressed,
            () => _animationService.SlideOut());

        _settingsApplier.ApplyAll();
        _settingsApplier.LoadToolsButtonIcon();
        PositionWindow();
    }

    public void ShowStartupAnimation()
    {
        _animationService.ShowStartupAnimation();
    }

    public void SlideIn()
    {
        _animationService.SlideIn();
    }

    public void SlideOut()
    {
        _animationService.SlideOut();
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

    public void OpenSettings()
    {
        var settingsWindow = new SettingsWindow(_configService, new AutoStartService());
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
            _settingsApplier.ApplyAll();

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
        _statusService.Dispose();
        _autoHideController.Stop();
        base.OnClosed(e);
    }
}
