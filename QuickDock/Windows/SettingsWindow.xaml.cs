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
        ToolsManageButton.Content = Lang.T("Settings.ToolsManage");

        AddButton.Content = Lang.T("Settings.Add");
        EditButton.Content = Lang.T("Settings.Edit");
        DeleteButton.Content = Lang.T("Settings.Delete");
        UpButton.Content = Lang.T("Settings.Up");
        DownButton.Content = Lang.T("Settings.Down");
        SaveButton.Content = Lang.T("Settings.Save");
        CancelButton.Content = Lang.T("Settings.Cancel");
        AboutVersionLabel.Text = Lang.T("Settings.AboutVersion");
        AboutConfigPathLabel.Text = Lang.T("Settings.AboutConfigPath");
        AboutThemeLabel.Text = Lang.T("Settings.AboutTheme");
        AboutThemeValue.Text = Lang.T("Settings.AboutThemeValue");
        AboutReloadLabel.Text = Lang.T("Settings.AboutReload");
        AboutReloadValue.Text = Lang.T("Settings.AboutReloadValue");
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
        var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AllowFullOpen = true
        };

        try
        {
            var currentColor = (WpfColor)WpfColorConverter.ConvertFromString(BackgroundColorTextBox.Text)!;
            dialog.Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B);
        }
        catch
        {
            dialog.Color = System.Drawing.Color.FromArgb(30, 30, 30);
        }

        if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
        {
            return;
        }

        BackgroundColorTextBox.Text = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
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
        var hasSelection = ItemsListBox.SelectedItem != null;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
        UpButton.IsEnabled = hasSelection;
        DownButton.IsEnabled = hasSelection;
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var editWindow = new ItemEditWindow();
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true)
        {
            if (editWindow.Item != null)
            {
                _items.Add(editWindow.Item);
                ItemsListBox.Items.Refresh();
            }
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (ItemsListBox.SelectedItem is not DockItem item) return;
        var editWindow = new ItemEditWindow(item);
        editWindow.Owner = this;
        if (editWindow.ShowDialog() == true)
        {
            var index = _items.IndexOf(item);
            if (editWindow.Item != null)
            {
                _items[index] = editWindow.Item;
            }
            ItemsListBox.Items.Refresh();
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (ItemsListBox.SelectedItem is not DockItem item) return;
        _items.Remove(item);
        ItemsListBox.Items.Refresh();
    }

    private void OnMoveUpClick(object sender, RoutedEventArgs e)
    {
        if (ItemsListBox.SelectedItem is not DockItem item) return;
        var index = _items.IndexOf(item);
        if (index <= 0) return;
        _items.RemoveAt(index);
        _items.Insert(index - 1, item);
        ItemsListBox.Items.Refresh();
        ItemsListBox.SelectedIndex = index - 1;
    }

    private void OnMoveDownClick(object sender, RoutedEventArgs e)
    {
        if (ItemsListBox.SelectedItem is not DockItem item) return;
        var index = _items.IndexOf(item);
        if (index >= _items.Count - 1) return;
        _items.RemoveAt(index);
        _items.Insert(index + 1, item);
        ItemsListBox.Items.Refresh();
        ItemsListBox.SelectedIndex = index + 1;
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
        }
    }

    private void OnToolsScanClick(object sender, RoutedEventArgs e)
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

        foreach (var confirmed in result.ConfirmedItems)
        {
            var existing = existingItems.FirstOrDefault(i => 
                i.ExePath.Equals(confirmed.ExePath, StringComparison.OrdinalIgnoreCase));
            if (existing != null)
                mergedItems.Add(existing);
            else
                mergedItems.Add(confirmed);
        }

        for (int i = 0; i < mergedItems.Count; i++)
            mergedItems[i].Order = i;

        _configService.Settings.ToolsItems = mergedItems;
        _configService.Settings.ToolsRootPath = rootPath;
        _pendingFolders = result.PendingFolders;

        UpdateToolsPendingWarning();

        var pendingCount = result.PendingFolders.Count;
        var msg = string.Format(Lang.T("Tools.ScanComplete"), mergedItems.Count);
        if (pendingCount > 0)
        {
            msg += string.Format(Lang.T("Tools.ScanHasPending"), pendingCount);
        }

        System.Windows.MessageBox.Show(msg, Title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnToolsGoToManageClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ToolsManageWindow(_configService, _scanService, _pendingFolders);
        dialog.Owner = this;
        if (dialog.ShowDialog() == true)
        {
            UpdateToolsPendingWarning();
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
        if (string.IsNullOrWhiteSpace(configDirectory))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = configDirectory,
            UseShellExecute = true
        });
    }

    private static string GetAppVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
    }
}
