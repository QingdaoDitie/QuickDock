using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QuickDock.Controls;
using QuickDock.Models;

using WpfButton = System.Windows.Controls.Button;
using WpfImage = System.Windows.Controls.Image;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;

namespace QuickDock.Services;

public class SettingsApplier
{
    private readonly Window _window;
    private readonly ConfigService _configService;
    private readonly FrameworkElement _mainGrid;
    private readonly Border _dockBorder;
    private readonly Border _toolsPanel;
    private readonly StatusControl _statusControl;
    private readonly Border _statusBarSeparator;
    private readonly WpfButton _toolsButton;
    private readonly ItemsControl _dockItems;
    private readonly ItemsControl _toolsItems;
    private readonly WpfImage _toolsButtonIcon;
    private readonly TextBlock _toolsButtonFallbackText;
    private readonly Func<int> _getToolsCount;

    public SettingsApplier(
        Window window,
        ConfigService configService,
        FrameworkElement mainGrid,
        Border dockBorder,
        Border toolsPanel,
        StatusControl statusControl,
        Border statusBarSeparator,
        WpfButton toolsButton,
        ItemsControl dockItems,
        ItemsControl toolsItems,
        WpfImage toolsButtonIcon,
        TextBlock toolsButtonFallbackText,
        Func<int> getToolsCount)
    {
        _window = window;
        _configService = configService;
        _mainGrid = mainGrid;
        _dockBorder = dockBorder;
        _toolsPanel = toolsPanel;
        _statusControl = statusControl;
        _statusBarSeparator = statusBarSeparator;
        _toolsButton = toolsButton;
        _dockItems = dockItems;
        _toolsItems = toolsItems;
        _toolsButtonIcon = toolsButtonIcon;
        _toolsButtonFallbackText = toolsButtonFallbackText;
        _getToolsCount = getToolsCount;
    }

    public void ApplyAll()
    {
        ApplyOpacity();
        ApplyBackgroundColor();
        ApplyScale();
        ApplyIconSpacing();
        ApplyStatusBarVisibility();
        ApplyToolsButtonVisibility();
        ApplyToolsIconSpacing();
        ApplyToolButtonSize();
        _toolsButton.ToolTip = Lang.T("Tools.ToolTip");
    }

    public void ApplyOpacity()
    {
        _window.Opacity = _configService.Settings.DockOpacity;
    }

    public void ApplyBackgroundColor()
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
        _dockBorder.Background = brush;
        _toolsPanel.Background = brush;
        _statusControl.RootBorder.Background = brush;
    }

    public void ApplyScale()
    {
        var scale = _configService.Settings.Scale;
        _mainGrid.LayoutTransform = new ScaleTransform(scale, scale);
    }

    public void ApplyIconSpacing()
    {
        var spacing = _configService.Settings.IconSpacing;
        var style = new Style(typeof(ContentPresenter));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(spacing / 2, 0, spacing / 2, 0)));
        _dockItems.ItemContainerStyle = style;
    }

    public void ApplyToolsIconSpacing()
    {
        var spacing = _configService.Settings.IconSpacing;
        var style = new Style(typeof(ContentPresenter));
        style.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(spacing / 2, 0, spacing / 2, 0)));
        _toolsItems.ItemContainerStyle = style;
    }

    public void ApplyStatusBarVisibility()
    {
        var visible = _configService.Settings.ShowStatusBar ? Visibility.Visible : Visibility.Collapsed;
        _statusControl.Visibility = visible;
        _statusBarSeparator.Visibility = visible;
    }

    public void ApplyToolsButtonVisibility()
    {
        _toolsButton.Visibility = _configService.Settings.ToolsEnabled && _getToolsCount() > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    public void ApplyToolButtonSize()
    {
        var size = _configService.Settings.ToolIconSize;
        _toolsButtonIcon.Width = size;
        _toolsButtonIcon.Height = size;
    }

    public void LoadToolsButtonIcon()
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

                _toolsButtonIcon.Source = bitmap;
                _toolsButtonIcon.Visibility = Visibility.Visible;
                _toolsButtonFallbackText.Visibility = Visibility.Collapsed;
                return;
            }
            catch
            {
            }
        }

        _toolsButtonIcon.Source = null;
        _toolsButtonIcon.Visibility = Visibility.Collapsed;
        _toolsButtonFallbackText.Visibility = Visibility.Visible;
    }
}
