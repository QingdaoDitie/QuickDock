using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using QuickDock.Models;
using QuickDock.Services;

using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfPoint = System.Windows.Point;
using WpfButton = System.Windows.Controls.Button;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfThickness = System.Windows.Thickness;
using WpfBrush = System.Windows.Media.Brush;
using WpfCornerRadius = System.Windows.CornerRadius;
using WpfFontWeights = System.Windows.FontWeights;

namespace QuickDock.Windows;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly AutoStartService _autoStartService;
    private readonly ToolsScanService _scanService;
    private List<DockItem> _items;
    private List<PendingToolFolder> _pendingFolders = new();
    private readonly AppSettings _settingsSnapshot;
    private bool _initialized;
    private bool _autoStartEnabled;
    private readonly bool _autoStartSnapshot;
    private bool _isSaving;
    private bool _isRefreshingLanguageChoices;
    private bool _isDragging;
    private DockItem? _dragItem;
    private System.Windows.Point _dragStartPoint;
    private bool _isToolDragging;
    private ToolItem? _dragToolItem;
    private WpfPoint _toolDragStartPoint;

    private System.Windows.Controls.Button? _activeNav;
    private readonly Dictionary<string, StackPanel> _pages = new();
    private readonly Dictionary<string, System.Windows.Controls.Button> _navButtons = new();

    public SettingsWindow(ConfigService configService, AutoStartService autoStartService)
    {
        _configService = configService;
        _autoStartService = autoStartService;
        _scanService = new ToolsScanService();
        _settingsSnapshot = _configService.Settings.Clone();
        _items = new List<DockItem>(_configService.Items);
        
        InitializeComponent();
        
        ItemsListBox.ItemsSource = _items;
        
        _activeNav = NavBasic;
        _pages["NavBasic"] = PageBasic;
        _pages["NavAppearance"] = PageAppearance;
        _pages["NavDockItems"] = PageDockItems;
        _pages["NavHotZone"] = PageHotZone;
        _pages["NavTools"] = PageTools;
        _pages["NavAbout"] = PageAbout;
        _navButtons["NavBasic"] = NavBasic;
        _navButtons["NavAppearance"] = NavAppearance;
        _navButtons["NavDockItems"] = NavDockItems;
        _navButtons["NavHotZone"] = NavHotZone;
        _navButtons["NavTools"] = NavTools;
        _navButtons["NavAbout"] = NavAbout;
        
        _autoStartEnabled = _autoStartService.IsEnabled();
        _autoStartSnapshot = _autoStartEnabled;
        _configService.Settings.AutoStart = _autoStartEnabled;
        UpdateAutoStartToggle();
        
        LanguageComboBox.Items.Add(Lang.T("Settings.Language.zh"));
        LanguageComboBox.Items.Add(Lang.T("Settings.Language.en"));
        
        var opacity = _configService.Settings.DockOpacity;
        OpacitySlider.Value = opacity;
        OpacityValue.Text = $"{(int)(opacity * 100)}%";
        
        LanguageComboBox.SelectedIndex = _configService.Settings.Language == "zh" ? 0 : 1;
        
        BackgroundColorTextBox.Text = _configService.Settings.BackgroundColor;
        UpdateBackgroundColorPreview(_configService.Settings.BackgroundColor);
        
        var scale = _configService.Settings.Scale;
        ScaleSlider.Value = scale;
        ScaleValue.Text = $"{(int)(scale * 100)}%";
        
        var iconSize = _configService.Settings.IconSize;
        IconSizeSlider.Value = iconSize;
        IconSizeValue.Text = $"{(int)iconSize}px";
        
        var iconSpacing = _configService.Settings.IconSpacing;
        IconSpacingSlider.Value = iconSpacing;
        IconSpacingValue.Text = $"{(int)iconSpacing}px";

        var toolIconSize = _configService.Settings.ToolIconSize;
        ToolIconSizeSlider.Value = toolIconSize;
        ToolIconSizeValue.Text = $"{(int)toolIconSize}px";

        DockShowAnimationSlider.Value = _configService.Settings.DockShowAnimationDuration;
        DockShowAnimationValue.Text = $"{_configService.Settings.DockShowAnimationDuration}ms";

        DockHideAnimationSlider.Value = _configService.Settings.DockHideAnimationDuration;
        DockHideAnimationValue.Text = $"{_configService.Settings.DockHideAnimationDuration}ms";

        ToolsExpandAnimationSlider.Value = _configService.Settings.ToolsExpandAnimationDuration;
        ToolsExpandAnimationValue.Text = $"{_configService.Settings.ToolsExpandAnimationDuration}ms";

        ToolsCollapseAnimationSlider.Value = _configService.Settings.ToolsCollapseAnimationDuration;
        ToolsCollapseAnimationValue.Text = $"{_configService.Settings.ToolsCollapseAnimationDuration}ms";

        StartupPreviewSlider.Value = _configService.Settings.StartupPreviewDuration;
        StartupPreviewValue.Text = $"{_configService.Settings.StartupPreviewDuration}ms";
        
        ShowStatusBarCheckBox.IsChecked = _configService.Settings.ShowStatusBar;
        WeatherCityTextBox.Text = _configService.Settings.WeatherCity;

        WeatherRefreshSlider.Value = _configService.Settings.WeatherRefreshIntervalMinutes;
        WeatherRefreshValue.Text = $"{_configService.Settings.WeatherRefreshIntervalMinutes}m";

        ResourceRefreshSlider.Value = _configService.Settings.ResourceRefreshIntervalSeconds;
        ResourceRefreshValue.Text = $"{_configService.Settings.ResourceRefreshIntervalSeconds}s";
        
        var triggerDelay = _configService.Settings.HotZoneTriggerDelay;
        TriggerDelaySlider.Value = triggerDelay;
        TriggerDelayValue.Text = $"{(int)triggerDelay}ms";
        
        var edgeSize = _configService.Settings.HotZoneEdgeSize;
        EdgeSizeSlider.Value = edgeSize;
        EdgeSizeValue.Text = $"{(int)edgeSize}px";

        AutoHideToleranceSlider.Value = _configService.Settings.AutoHideTolerance;
        AutoHideToleranceValue.Text = $"{_configService.Settings.AutoHideTolerance}px";
        
        ToolsEnabledCheckBox.IsChecked = _configService.Settings.ToolsEnabled;
        ToolsRootPathTextBox.Text = _configService.Settings.ToolsRootPath;
        LoadPendingToolsPanel();
        RefreshToolsList();
        UpdateToolsPendingWarning();
        AboutVersionValue.Text = GetAppVersion();
        AboutConfigPathValue.Text = _configService.GetConfigPath();
        
        _initialized = true;
        ApplyLanguage();
    }

    private void OnNavClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button clicked) return;

        foreach (var kvp in _pages)
        {
            kvp.Value.Visibility = Visibility.Collapsed;
        }
        foreach (var kvp in _navButtons)
        {
            kvp.Value.Style = (Style)FindResource("NavButtonStyle");
        }

        _pages[clicked.Name].Visibility = Visibility.Visible;
        clicked.Style = (Style)FindResource("ActiveNavButtonStyle");
        _activeNav = clicked;
    }

    private void ApplyLanguage()
    {
        Title = Lang.T("Settings.Title");
        NavBasic.Content = Lang.T("Settings.Nav.Basic");
        NavAppearance.Content = Lang.T("Settings.Nav.Appearance");
        NavDockItems.Content = Lang.T("Settings.Nav.DockItems");
        NavHotZone.Content = Lang.T("Settings.Nav.HotZone");
        NavTools.Content = Lang.T("Settings.Nav.Tools");
        NavAbout.Content = Lang.T("Settings.Nav.About");
        
        BasicTitle.Text = NavBasic.Content as string;
        BasicSubtitle.Text = Lang.T("Settings.BasicSubtitle");
        AppearanceTitle.Text = NavAppearance.Content as string;
        AppearanceSubtitle.Text = Lang.T("Settings.AppearanceSubtitle");
        DockItemsLabel.Text = NavDockItems.Content as string;
        DockItemsSubtitle.Text = Lang.T("Settings.DockItemsSubtitle");
        HotZoneTitle.Text = NavHotZone.Content as string;
        HotZoneSubtitle.Text = Lang.T("Settings.HotZoneSubtitle");
        ToolsSectionLabel.Text = NavTools.Content as string;
        ToolsSubtitle.Text = Lang.T("Settings.ToolsSubtitle");
        AboutTitle.Text = NavAbout.Content as string;
        AboutSubtitle.Text = Lang.T("Settings.AboutSubtitle");
        
        LanguageLabel.Text = Lang.T("Settings.Language");
        AutoStartTitleText.Text = Lang.T("Settings.AutoStartTitle");
        AutoStartDescText.Text = Lang.T("Settings.AutoStartDesc");
        ShowStatusBarLabel.Text = Lang.T("Settings.StatusBar");
        WeatherCityLabel.Text = Lang.T("Settings.WeatherCity");
        WeatherRefreshLabel.Text = Lang.T("Settings.WeatherRefresh");
        ResourceRefreshLabel.Text = Lang.T("Settings.ResourceRefresh");
        
        BackgroundColorLabel.Text = Lang.T("Settings.Background");
        ColorPresetsLabel.Text = Lang.T("Settings.ColorPresets");
        PickBackgroundColorButton.Content = Lang.T("Settings.PickColor");
        OpacityLabel.Text = Lang.T("Settings.Opacity");
        ScaleLabel.Text = Lang.T("Settings.Scale");
        IconSizeLabel.Text = Lang.T("Settings.IconSize");
        IconSpacingLabel.Text = Lang.T("Settings.Spacing");
        DockShowAnimationLabel.Text = Lang.T("Settings.DockShowAnimation");
        DockHideAnimationLabel.Text = Lang.T("Settings.DockHideAnimation");
        ToolsExpandAnimationLabel.Text = Lang.T("Settings.ToolsExpandAnimation");
        ToolsCollapseAnimationLabel.Text = Lang.T("Settings.ToolsCollapseAnimation");
        StartupPreviewLabel.Text = Lang.T("Settings.StartupPreview");
        ResetAppearanceButton.Content = Lang.T("Settings.ResetAppearance");
        
        TriggerDelayLabel.Text = Lang.T("Settings.TriggerDelay");
        EdgeSizeLabel.Text = Lang.T("Settings.EdgeSize");
        AutoHideToleranceLabel.Text = Lang.T("Settings.AutoHideTolerance");
        
        ToolsEnabledLabel.Text = Lang.T("Settings.Enabled");
        ToolIconSizeLabel.Text = Lang.T("Settings.ToolIcon");
        ToolsRootPathLabel.Text = Lang.T("Settings.Path");
        ToolsBrowseButton.Content = Lang.T("Settings.ToolsBrowse");
        ToolsScanButton.Content = Lang.T("Settings.ToolsScan");
        ToolsPendingSectionLabel.Text = Lang.CurrentLanguage == QuickDock.Services.Language.Chinese ? "待确认工具" : "Pending Tools";
        ToolsManageSectionLabel.Text = Lang.CurrentLanguage == QuickDock.Services.Language.Chinese ? "已添加工具" : "Managed Tools";

        AddButton.Content = Lang.T("Settings.Add");
        SaveButton.Content = Lang.T("Settings.Save");
        CancelButton.Content = Lang.T("Settings.Cancel");
        AboutConfigPathLabel.Text = Lang.T("Settings.AboutConfigPath");
        AboutThemeValue.Text = Lang.T("Settings.AboutThemeValue");
        OpenConfigFolderButton.Content = Lang.T("Settings.AboutOpenConfig");
        RefreshLanguageChoices();
        UpdateToolsPendingWarning();
    }

    private void RefreshLanguageChoices()
    {
        _isRefreshingLanguageChoices = true;
        try
        {
            var selectedIndex = LanguageComboBox.SelectedIndex < 0 ? 0 : LanguageComboBox.SelectedIndex;
            LanguageComboBox.Items.Clear();
            LanguageComboBox.Items.Add(Lang.T("Settings.Language.zh"));
            LanguageComboBox.Items.Add(Lang.T("Settings.Language.en"));
            LanguageComboBox.SelectedIndex = selectedIndex;
        }
        finally
        {
            _isRefreshingLanguageChoices = false;
        }
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || _isRefreshingLanguageChoices) return;
        _configService.Settings.Language = LanguageComboBox.SelectedIndex == 0 ? "zh" : "en";
        ApplyLanguage();
    }

    private void OnAutoStartClick(object sender, MouseButtonEventArgs e)
    {
        _autoStartEnabled = !_autoStartEnabled;
        _configService.Settings.AutoStart = _autoStartEnabled;
        if (_autoStartEnabled)
            _autoStartService.Enable();
        else
            _autoStartService.Disable();
        UpdateAutoStartToggle();
    }

    private void UpdateAutoStartToggle()
    {
        if (_autoStartEnabled)
        {
            AutoStartToggle.Background = (System.Windows.Media.Brush)FindResource("ToggleOnBrush");
            AutoStartToggleKnob.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Right);
            AutoStartToggleKnob.Margin = new Thickness(0, 0, 2, 0);
        }
        else
        {
            AutoStartToggle.Background = (System.Windows.Media.Brush)FindResource("ToggleOffBrush");
            AutoStartToggleKnob.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Left);
            AutoStartToggleKnob.Margin = new Thickness(2, 0, 0, 0);
        }
    }

    private void OnBackgroundColorChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized) return;
        var text = BackgroundColorTextBox.Text;
        _configService.Settings.BackgroundColor = text;
        UpdateBackgroundColorPreview(text);
    }

    private void UpdateBackgroundColorPreview(string colorText)
    {
        try
        {
            var color = (WpfColor)WpfColorConverter.ConvertFromString(colorText)!;
            BackgroundColorPreview.Background = new SolidColorBrush(color);
        }
        catch
        {
            BackgroundColorPreview.Background = new SolidColorBrush(Colors.Transparent);
        }
    }

    private void OnBackgroundPresetClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not string colorText)
        {
            return;
        }

        BackgroundColorTextBox.Text = colorText;
    }

    private void OnPickBackgroundColorClick(object sender, RoutedEventArgs e)
    {
        PickColorForTextBox(BackgroundColorTextBox, System.Drawing.Color.FromArgb(30, 30, 30));
    }

    private void PickColorForTextBox(System.Windows.Controls.TextBox textBox, System.Drawing.Color fallbackColor)
    {
        var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AllowFullOpen = true
        };

        try
        {
            var currentColor = (WpfColor)WpfColorConverter.ConvertFromString(textBox.Text)!;
            dialog.Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B);
        }
        catch
        {
            dialog.Color = fallbackColor;
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        textBox.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
    }

    private void OnResetAppearanceClick(object sender, RoutedEventArgs e)
    {
        BackgroundColorTextBox.Text = "#1e1e1e";
        OpacitySlider.Value = 0.9;
        ScaleSlider.Value = 1.0;
        IconSizeSlider.Value = 32;
        IconSpacingSlider.Value = 5;
        ToolIconSizeSlider.Value = 24;
        DockShowAnimationSlider.Value = 220;
        DockHideAnimationSlider.Value = 180;
        ToolsExpandAnimationSlider.Value = 140;
        ToolsCollapseAnimationSlider.Value = 110;
        StartupPreviewSlider.Value = 1500;
    }

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || OpacityValue == null) return;
        _configService.Settings.DockOpacity = OpacitySlider.Value;
        OpacityValue.Text = $"{(int)(OpacitySlider.Value * 100)}%";
    }

    private void OnScaleChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || ScaleValue == null) return;
        _configService.Settings.Scale = ScaleSlider.Value;
        ScaleValue.Text = $"{(int)(ScaleSlider.Value * 100)}%";
    }

    private void OnIconSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || IconSizeValue == null) return;
        _configService.Settings.IconSize = IconSizeSlider.Value;
        IconSizeValue.Text = $"{(int)IconSizeSlider.Value}px";
    }

    private void OnIconSpacingChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || IconSpacingValue == null) return;
        _configService.Settings.IconSpacing = IconSpacingSlider.Value;
        IconSpacingValue.Text = $"{(int)IconSpacingSlider.Value}px";
    }

    private void OnToolIconSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || ToolIconSizeValue == null) return;
        _configService.Settings.ToolIconSize = ToolIconSizeSlider.Value;
        ToolIconSizeValue.Text = $"{(int)ToolIconSizeSlider.Value}px";
    }

    private void OnDockShowAnimationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || DockShowAnimationValue == null) return;
        _configService.Settings.DockShowAnimationDuration = (int)Math.Round(DockShowAnimationSlider.Value);
        DockShowAnimationValue.Text = $"{_configService.Settings.DockShowAnimationDuration}ms";
    }

    private void OnDockHideAnimationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || DockHideAnimationValue == null) return;
        _configService.Settings.DockHideAnimationDuration = (int)Math.Round(DockHideAnimationSlider.Value);
        DockHideAnimationValue.Text = $"{_configService.Settings.DockHideAnimationDuration}ms";
    }

    private void OnToolsExpandAnimationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || ToolsExpandAnimationValue == null) return;
        _configService.Settings.ToolsExpandAnimationDuration = (int)Math.Round(ToolsExpandAnimationSlider.Value);
        ToolsExpandAnimationValue.Text = $"{_configService.Settings.ToolsExpandAnimationDuration}ms";
    }

    private void OnToolsCollapseAnimationChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || ToolsCollapseAnimationValue == null) return;
        _configService.Settings.ToolsCollapseAnimationDuration = (int)Math.Round(ToolsCollapseAnimationSlider.Value);
        ToolsCollapseAnimationValue.Text = $"{_configService.Settings.ToolsCollapseAnimationDuration}ms";
    }

    private void OnStartupPreviewChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || StartupPreviewValue == null) return;
        _configService.Settings.StartupPreviewDuration = (int)Math.Round(StartupPreviewSlider.Value);
        StartupPreviewValue.Text = $"{_configService.Settings.StartupPreviewDuration}ms";
    }

    private void OnTriggerDelayChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || TriggerDelayValue == null) return;
        _configService.Settings.HotZoneTriggerDelay = (int)TriggerDelaySlider.Value;
        TriggerDelayValue.Text = $"{(int)TriggerDelaySlider.Value}ms";
    }

    private void OnEdgeSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || EdgeSizeValue == null) return;
        _configService.Settings.HotZoneEdgeSize = (int)EdgeSizeSlider.Value;
        EdgeSizeValue.Text = $"{(int)EdgeSizeSlider.Value}px";
    }

    private void OnAutoHideToleranceChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || AutoHideToleranceValue == null) return;
        _configService.Settings.AutoHideTolerance = (int)Math.Round(AutoHideToleranceSlider.Value);
        AutoHideToleranceValue.Text = $"{_configService.Settings.AutoHideTolerance}px";
    }

    private void OnShowStatusBarChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _configService.Settings.ShowStatusBar = ShowStatusBarCheckBox.IsChecked == true;
    }

    private void OnWeatherCityChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized) return;
        _configService.Settings.WeatherCity = WeatherCityTextBox.Text;
    }

    private void OnWeatherRefreshChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || WeatherRefreshValue == null) return;
        _configService.Settings.WeatherRefreshIntervalMinutes = (int)Math.Round(WeatherRefreshSlider.Value);
        WeatherRefreshValue.Text = $"{_configService.Settings.WeatherRefreshIntervalMinutes}m";
    }

    private void OnResourceRefreshChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || ResourceRefreshValue == null) return;
        _configService.Settings.ResourceRefreshIntervalSeconds = (int)Math.Round(ResourceRefreshSlider.Value);
        ResourceRefreshValue.Text = $"{_configService.Settings.ResourceRefreshIntervalSeconds}s";
    }

    private void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
    }

    private WpfBrush Brush(string key) => (WpfBrush)FindResource(key);

    private void OnItemDragStart(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is WpfButton) return;
        _dragStartPoint = e.GetPosition(null);
        _dragItem = null;
        _isDragging = false;

        if (ItemsListBox.ContainerFromElement((DependencyObject)e.OriginalSource) is ListBoxItem lbi && lbi.Content is DockItem item)
        {
            _dragItem = item;
        }
    }

    private void OnItemDragging(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragItem == null || _isDragging) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(null);
        var diff = _dragStartPoint - pos;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _isDragging = true;
        var index = _items.IndexOf(_dragItem);
        if (index < 0) return;

        var effect = DragDrop.DoDragDrop(ItemsListBox, _dragItem, WpfDragDropEffects.Move);
        if (effect == WpfDragDropEffects.Move)
        {
            ItemsListBox.ItemsSource = null;
            ItemsListBox.ItemsSource = _items;
            if (index < _items.Count)
                ItemsListBox.SelectedIndex = index;
        }
        _isDragging = false;
        _dragItem = null;
    }

    private void OnItemDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(DockItem)))
        {
            e.Effects = WpfDragDropEffects.None;
            e.Handled = true;
            return;
        }

        var dropItem = (DockItem?)e.Data.GetData(typeof(DockItem));
        if (dropItem == null) return;

        var targetItem = GetItemAtDragPosition((System.Windows.Controls.ListBox)sender, e.GetPosition((IInputElement)sender));
        if (targetItem == null || targetItem == dropItem) return;

        var oldIndex = _items.IndexOf(dropItem);
        var newIndex = _items.IndexOf(targetItem);
        if (oldIndex < 0 || newIndex < 0) return;

        _items.RemoveAt(oldIndex);
        _items.Insert(newIndex, dropItem);
        ItemsListBox.Items.Refresh();
        ItemsListBox.SelectedItem = dropItem;

        e.Effects = WpfDragDropEffects.Move;
        e.Handled = true;
    }

    private void OnItemDrop(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(DockItem)) ? WpfDragDropEffects.Move : WpfDragDropEffects.None;
        e.Handled = true;
    }

    private DockItem? GetItemAtDragPosition(System.Windows.Controls.ListBox listBox, System.Windows.Point position)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (listBox.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem lbi)
            {
                var rect = VisualTreeHelper.GetDescendantBounds(lbi);
                var pos = listBox.TranslatePoint(position, lbi);
                if (rect.Contains(pos))
                    return _items[i];
            }
        }
        return null;
    }

    private void OnItemDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging) return;
        if (ItemsListBox.SelectedItem is not DockItem item) return;
        var editWindow = new ItemEditWindow(item);
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true && editWindow.Item != null)
        {
            var index = _items.IndexOf(item);
            _items[index] = editWindow.Item;
            ItemsListBox.Items.Refresh();
        }
    }

    private void OnDeleteItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is WpfButton btn && btn.Tag is DockItem item)
        {
            _items.Remove(item);
            ItemsListBox.Items.Refresh();
        }
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var editWindow = new ItemEditWindow();
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true && editWindow.Item != null)
        {
            _items.Add(editWindow.Item);
            ItemsListBox.Items.Refresh();
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _configService.Settings.BackgroundColor = BackgroundColorTextBox.Text;
        _configService.Settings.ToolsEnabled = ToolsEnabledCheckBox.IsChecked == true;
        _configService.Settings.ToolsRootPath = ToolsRootPathTextBox.Text;
        
        _configService.Items.Clear();
        foreach (var item in _items)
        {
            _configService.Items.Add(item);
        }
        _isSaving = true;
        _configService.Save();
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isSaving)
        {
            _configService.Settings.CopyFrom(_settingsSnapshot);
            if (_autoStartSnapshot != _autoStartEnabled)
            {
                if (_autoStartSnapshot)
                    _autoStartService.Enable();
                else
                    _autoStartService.Disable();
            }
        }

        base.OnClosing(e);
    }

    private void OnToolsEnabledChanged(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        _configService.Settings.ToolsEnabled = ToolsEnabledCheckBox.IsChecked == true;
    }

    private void OnToolsBrowseClick(object sender, RoutedEventArgs e)
    {
        var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = Lang.CurrentLanguage == QuickDock.Services.Language.Chinese ? "选择工具文件夹" : "Select Tools Folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        if (!string.IsNullOrEmpty(ToolsRootPathTextBox.Text) &&
            System.IO.Directory.Exists(ToolsRootPathTextBox.Text))
        {
            dialog.SelectedPath = ToolsRootPathTextBox.Text;
        }

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ToolsRootPathTextBox.Text = dialog.SelectedPath;
            _configService.Settings.ToolsRootPath = dialog.SelectedPath;
            PerformScan();
        }
    }

    private void OnToolsScanClick(object sender, RoutedEventArgs e)
    {
        PerformScan();
    }

    private void PerformScan()
    {
        var rootPath = ToolsRootPathTextBox.Text;
        if (string.IsNullOrWhiteSpace(rootPath) || !System.IO.Directory.Exists(rootPath))
        {
            System.Windows.MessageBox.Show(
                Lang.T("Tools.PathNotExist"),
                Title, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = _scanService.Scan(rootPath);
        var existingItems = _configService.Settings.ToolsItems;
        var mergedItems = new List<ToolItem>();

        foreach (var existing in existingItems.Where(i => i.IsConfirmed))
        {
            if (System.IO.File.Exists(existing.ExePath))
            {
                mergedItems.Add(existing);
            }
        }

        foreach (var confirmed in result.ConfirmedItems)
        {
            var alreadyExists = mergedItems.Any(i =>
                i.ExePath.Equals(confirmed.ExePath, StringComparison.OrdinalIgnoreCase));
            if (!alreadyExists)
            {
                mergedItems.Add(confirmed);
            }
        }

        for (int i = 0; i < mergedItems.Count; i++)
            mergedItems[i].Order = i;

        foreach (var pending in result.PendingFolders)
        {
            pending.Candidates = pending.Candidates
                .Where(c => !mergedItems.Any(m =>
                    m.ExePath.Equals(c, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
        result.PendingFolders.RemoveAll(p => p.Candidates.Count == 0);

        foreach (var pending in result.PendingFolders.ToList())
        {
            if (pending.Candidates.Count == 1)
            {
                mergedItems.Add(new ToolItem
                {
                    DisplayName = System.IO.Path.GetFileNameWithoutExtension(pending.Candidates[0]),
                    ExePath = pending.Candidates[0],
                    SourceFolder = pending.FolderPath,
                    IsConfirmed = true,
                    Order = mergedItems.Count
                });
                result.PendingFolders.Remove(pending);
            }
        }

        _configService.Settings.ToolsItems = mergedItems;
        _configService.Settings.ToolsRootPath = rootPath;
        _pendingFolders = result.PendingFolders;

        LoadPendingToolsPanel();
        RefreshToolsList();
        UpdateToolsPendingWarning();

        var pendingCount = result.PendingFolders.Count;
        var msg = string.Format(Lang.T("Tools.ScanComplete"), mergedItems.Count);
        if (pendingCount > 0)
        {
            msg += string.Format(Lang.T("Tools.ScanHasPending"), pendingCount);
        }

        System.Windows.MessageBox.Show(msg, Title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LoadPendingToolsPanel()
    {
        ToolsPendingPanel.Children.Clear();

        foreach (var pending in _pendingFolders)
        {
            var border = new Border
            {
                Background = Brush("WarningBackgroundBrush"),
                BorderBrush = Brush("WarningBrush"),
                BorderThickness = new WpfThickness(1),
                CornerRadius = new WpfCornerRadius(4),
                Padding = new WpfThickness(10, 8, 10, 8),
                Margin = new WpfThickness(0, 0, 0, 6)
            };

            var stack = new StackPanel();
            var header = new StackPanel { Orientation = WpfOrientation.Horizontal };
            header.Children.Add(new TextBlock
            {
                Text = pending.FolderName,
                FontSize = 12,
                FontWeight = WpfFontWeights.Medium,
                Foreground = Brush("TextBrush")
            });
            header.Children.Add(new TextBlock
            {
                Text = Lang.T("Tools.Pending"),
                FontSize = 10,
                Foreground = Brush("WarningBrush"),
                Margin = new WpfThickness(8, 0, 0, 0)
            });
            stack.Children.Add(header);

            stack.Children.Add(new TextBlock
            {
                Text = Lang.T("Tools.SelectMainExe") + ":",
                FontSize = 11,
                Foreground = Brush("MutedTextBrush"),
                Margin = new WpfThickness(0, 4, 0, 2)
            });

            var radioPanel = new StackPanel { Tag = pending };
            foreach (var candidate in pending.Candidates)
            {
                radioPanel.Children.Add(new WpfRadioButton
                {
                    Content = System.IO.Path.GetFileName(candidate),
                    GroupName = pending.FolderName,
                    Tag = candidate,
                    Foreground = Brush("TextBrush"),
                    Margin = new WpfThickness(0, 1, 0, 1)
                });
            }

            stack.Children.Add(radioPanel);

            var confirmButton = new WpfButton
            {
                Content = Lang.T("Tools.Confirm"),
                Style = (Style)FindResource("SuccessButtonStyle"),
                Padding = new WpfThickness(10, 3, 10, 3),
                Margin = new WpfThickness(0, 6, 0, 0),
                Tag = radioPanel
            };
            confirmButton.Click += OnConfirmPendingTool;
            stack.Children.Add(confirmButton);

            border.Child = stack;
            ToolsPendingPanel.Children.Add(border);
        }

        ToolsPendingPanel.Visibility = _pendingFolders.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshToolsList()
    {
        var orderedTools = _configService.Settings.ToolsItems
            .Where(t => t.IsConfirmed)
            .OrderBy(t => t.Order)
            .ToList();

        ToolsListBox.ItemsSource = null;
        ToolsListBox.ItemsSource = orderedTools;
    }

    private void OnConfirmPendingTool(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton button || button.Tag is not StackPanel radioPanel || radioPanel.Tag is not PendingToolFolder pending)
        {
            return;
        }

        string? selectedExe = null;
        foreach (var child in radioPanel.Children)
        {
            if (child is WpfRadioButton radio && radio.IsChecked == true)
            {
                selectedExe = radio.Tag as string;
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(selectedExe))
        {
            return;
        }

        _configService.Settings.ToolsItems.Add(new ToolItem
        {
            DisplayName = System.IO.Path.GetFileNameWithoutExtension(selectedExe),
            ExePath = selectedExe,
            SourceFolder = pending.FolderPath,
            IsConfirmed = true,
            Order = _configService.Settings.ToolsItems.Count
        });

        NormalizeToolOrder();
        _pendingFolders.Remove(pending);
        LoadPendingToolsPanel();
        RefreshToolsList();
        UpdateToolsPendingWarning();
    }

    private void OnRemoveToolClick(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton button || button.Tag is not ToolItem tool)
        {
            return;
        }

        _configService.Settings.ToolsItems.RemoveAll(t => t.Id == tool.Id);
        NormalizeToolOrder();
        RefreshToolsList();
        UpdateToolsPendingWarning();
    }

    private void OnToolDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_isToolDragging) return;
        if (ToolsListBox.SelectedItem is not ToolItem tool) return;

        var nameInput = new System.Windows.Controls.TextBox
        {
            Text = tool.DisplayName,
            Style = (Style)FindResource("TextBoxStyle"),
            Margin = new WpfThickness(0, 0, 0, 8)
        };

        var pathInput = new System.Windows.Controls.TextBox
        {
            Text = tool.ExePath,
            Style = (Style)FindResource("TextBoxStyle")
        };

        var dialog = new Window
        {
            Title = Lang.T("Tools.Edit"),
            Width = 420,
            Height = 180,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            Background = Brush("BackgroundBrush"),
            Foreground = Brush("TextBrush"),
            Style = (Style)FindResource("WindowStyle")
        };

        var panel = new StackPanel { Margin = new WpfThickness(16) };
        panel.Children.Add(new TextBlock { Text = Lang.T("Edit.Name"), Foreground = Brush("MutedTextBrush"), Margin = new WpfThickness(0, 0, 0, 4) });
        panel.Children.Add(nameInput);
        panel.Children.Add(new TextBlock { Text = Lang.T("Settings.Path"), Foreground = Brush("MutedTextBrush"), Margin = new WpfThickness(0, 0, 0, 4) });
        panel.Children.Add(pathInput);

        var actions = new StackPanel { Orientation = WpfOrientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new WpfThickness(0, 12, 0, 0) };
        var okButton = new WpfButton { Content = Lang.T("Settings.Save"), Style = (Style)FindResource("PrimaryActionButtonStyle"), Padding = new WpfThickness(20, 5, 20, 5), Margin = new WpfThickness(0, 0, 8, 0) };
        okButton.Click += (_, _) =>
        {
            tool.DisplayName = nameInput.Text.Trim();
            tool.ExePath = pathInput.Text.Trim();
            RefreshToolsList();
            dialog.DialogResult = true;
        };
        var cancelButton = new WpfButton { Content = Lang.T("Settings.Cancel"), Style = (Style)FindResource("SettingsButtonStyle"), Padding = new WpfThickness(20, 5, 20, 5) };
        cancelButton.Click += (_, _) => dialog.DialogResult = false;
        actions.Children.Add(okButton);
        actions.Children.Add(cancelButton);
        panel.Children.Add(actions);

        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private void OnToolDragStart(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is WpfButton) return;

        _toolDragStartPoint = e.GetPosition(null);
        _dragToolItem = null;
        _isToolDragging = false;

        if (ToolsListBox.ContainerFromElement((DependencyObject)e.OriginalSource) is ListBoxItem lbi && lbi.Content is ToolItem tool)
        {
            _dragToolItem = tool;
        }
    }

    private void OnToolDragging(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragToolItem == null || _isToolDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var position = e.GetPosition(null);
        var diff = _toolDragStartPoint - position;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        _isToolDragging = true;
        DragDrop.DoDragDrop(ToolsListBox, _dragToolItem, WpfDragDropEffects.Move);
        _isToolDragging = false;
        _dragToolItem = null;
    }

    private void OnToolDragOver(object sender, System.Windows.DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(ToolItem)))
        {
            e.Effects = WpfDragDropEffects.None;
            e.Handled = true;
            return;
        }

        var dropItem = (ToolItem?)e.Data.GetData(typeof(ToolItem));
        if (dropItem == null) return;

        var targetItem = GetToolAtDragPosition((WpfListBox)sender, e.GetPosition((IInputElement)sender));
        if (targetItem == null || targetItem == dropItem) return;

        var tools = _configService.Settings.ToolsItems.Where(t => t.IsConfirmed).OrderBy(t => t.Order).ToList();
        var oldIndex = tools.FindIndex(t => t.Id == dropItem.Id);
        var newIndex = tools.FindIndex(t => t.Id == targetItem.Id);
        if (oldIndex < 0 || newIndex < 0) return;

        tools.RemoveAt(oldIndex);
        tools.Insert(newIndex, dropItem);

        _configService.Settings.ToolsItems = tools;
        NormalizeToolOrder();
        RefreshToolsList();
        ToolsListBox.SelectedItem = dropItem;

        e.Effects = WpfDragDropEffects.Move;
        e.Handled = true;
    }

    private void OnToolDrop(object sender, System.Windows.DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(ToolItem)) ? WpfDragDropEffects.Move : WpfDragDropEffects.None;
        e.Handled = true;
    }

    private ToolItem? GetToolAtDragPosition(WpfListBox listBox, WpfPoint position)
    {
        var tools = _configService.Settings.ToolsItems.Where(t => t.IsConfirmed).OrderBy(t => t.Order).ToList();
        for (int i = 0; i < tools.Count; i++)
        {
            if (listBox.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem lbi)
            {
                var rect = VisualTreeHelper.GetDescendantBounds(lbi);
                var pos = listBox.TranslatePoint(position, lbi);
                if (rect.Contains(pos))
                {
                    return tools[i];
                }
            }
        }

        return null;
    }

    private void NormalizeToolOrder()
    {
        var confirmedTools = _configService.Settings.ToolsItems.Where(t => t.IsConfirmed).ToList();
        for (int i = 0; i < confirmedTools.Count; i++)
        {
            confirmedTools[i].Order = i;
        }
    }

    private void UpdateToolsPendingWarning()
    {
        var pendingCount = _pendingFolders.Count + _configService.Settings.ToolsItems.Count(t => !t.IsConfirmed);
        if (pendingCount > 0)
        {
            ToolsPendingWarning.Visibility = Visibility.Visible;
            ToolsPendingWarningText.Text = string.Format(Lang.T("Settings.ToolsPendingWarning"), pendingCount);
        }
        else
        {
            ToolsPendingWarning.Visibility = Visibility.Collapsed;
        }
    }

    private void OnOpenConfigFolderClick(object sender, RoutedEventArgs e)
    {
        var configDirectory = System.IO.Path.GetDirectoryName(_configService.GetConfigPath());
        if (string.IsNullOrWhiteSpace(configDirectory)) return;

        Process.Start(new ProcessStartInfo
        {
            FileName = configDirectory,
            UseShellExecute = true
        });
    }

    private void OnQuickDockTitleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
            Process.Start(new ProcessStartInfo("https://www.qingdaoditie.site/") { UseShellExecute = true });
    }

    private void OnLogoClick(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 2)
            Process.Start(new ProcessStartInfo("https://www.qingdaoditie.site/") { UseShellExecute = true });
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }
}
