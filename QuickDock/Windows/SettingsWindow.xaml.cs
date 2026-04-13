using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
    private bool _initialized;
    private bool _autoStartEnabled;

    private System.Windows.Controls.Button? _activeNav;
    private readonly Dictionary<string, StackPanel> _pages = new();
    private readonly Dictionary<string, System.Windows.Controls.Button> _navButtons = new();

    public SettingsWindow(ConfigService configService, AutoStartService autoStartService)
    {
        _configService = configService;
        _autoStartService = autoStartService;
        _scanService = new ToolsScanService();
        _items = new List<DockItem>(_configService.Items);
        
        InitializeComponent();
        
        ItemsListBox.ItemsSource = _items;
        
        _activeNav = NavBasic;
        _pages["NavBasic"] = PageBasic;
        _pages["NavAppearance"] = PageAppearance;
        _pages["NavDockItems"] = PageDockItems;
        _pages["NavHotZone"] = PageHotZone;
        _pages["NavTools"] = PageTools;
        _navButtons["NavBasic"] = NavBasic;
        _navButtons["NavAppearance"] = NavAppearance;
        _navButtons["NavDockItems"] = NavDockItems;
        _navButtons["NavHotZone"] = NavHotZone;
        _navButtons["NavTools"] = NavTools;
        
        _autoStartEnabled = _autoStartService.IsEnabled();
        UpdateAutoStartToggle();
        
        LanguageComboBox.Items.Add("中文");
        LanguageComboBox.Items.Add("English");
        
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
        
        ShowStatusBarCheckBox.IsChecked = _configService.Settings.ShowStatusBar;
        WeatherCityTextBox.Text = _configService.Settings.WeatherCity;
        
        var triggerDelay = _configService.Settings.HotZoneTriggerDelay;
        TriggerDelaySlider.Value = triggerDelay;
        TriggerDelayValue.Text = $"{(int)triggerDelay}ms";
        
        var edgeSize = _configService.Settings.HotZoneEdgeSize;
        EdgeSizeSlider.Value = edgeSize;
        EdgeSizeValue.Text = $"{(int)edgeSize}px";
        
        ToolsEnabledCheckBox.IsChecked = _configService.Settings.ToolsEnabled;
        ToolsRootPathTextBox.Text = _configService.Settings.ToolsRootPath;
        UpdateToolsPendingWarning();
        
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
        var lang = _configService.Settings.Language;
        
        NavBasic.Content = lang == "zh" ? "基础设置" : "Basic";
        NavAppearance.Content = lang == "zh" ? "外观设置" : "Appearance";
        NavDockItems.Content = lang == "zh" ? "快捷方式" : "Dock Items";
        NavHotZone.Content = lang == "zh" ? "热区设置" : "Hot Zone";
        NavTools.Content = lang == "zh" ? "工具集合" : "Tools";
        
        BasicTitle.Text = NavBasic.Content as string;
        AppearanceTitle.Text = NavAppearance.Content as string;
        DockItemsLabel.Text = NavDockItems.Content as string;
        HotZoneTitle.Text = NavHotZone.Content as string;
        ToolsSectionLabel.Text = NavTools.Content as string;
        
        LanguageLabel.Text = lang == "zh" ? "语言" : "Language";
        AutoStartTitleText.Text = lang == "zh" ? "开机自启" : "Auto Start";
        AutoStartDescText.Text = lang == "zh" ? "启动 Windows 时自动运行" : "Launch on Windows startup";
        ShowStatusBarLabel.Text = lang == "zh" ? "状态栏" : "Status Bar";
        WeatherCityLabel.Text = lang == "zh" ? "天气城市" : "Weather City";
        
        BackgroundColorLabel.Text = lang == "zh" ? "背景色" : "Background";
        OpacityLabel.Text = lang == "zh" ? "透明度" : "Opacity";
        ScaleLabel.Text = lang == "zh" ? "缩放" : "Scale";
        IconSizeLabel.Text = lang == "zh" ? "图标大小" : "Icon Size";
        IconSpacingLabel.Text = lang == "zh" ? "图标间距" : "Spacing";
        
        TriggerDelayLabel.Text = lang == "zh" ? "触发延迟" : "Trigger Delay";
        EdgeSizeLabel.Text = lang == "zh" ? "边缘范围" : "Edge Size";
        
        ToolsEnabledLabel.Text = lang == "zh" ? "启用" : "Enabled";
        ToolsRootPathLabel.Text = lang == "zh" ? "路径" : "Path";
        ToolsBrowseButton.Content = lang == "zh" ? "..." : "...";
        ToolsScanButton.Content = lang == "zh" ? "重新扫描" : "Rescan";
        ToolsManageButton.Content = lang == "zh" ? "管理工具" : "Manage";
        
        SaveButton.Content = lang == "zh" ? "保存" : "Save";
        CancelButton.Content = lang == "zh" ? "取消" : "Cancel";
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized) return;
        _configService.Settings.Language = LanguageComboBox.SelectedIndex == 0 ? "zh" : "en";
        ApplyLanguage();
    }

    private void OnAutoStartClick(object sender, MouseButtonEventArgs e)
    {
        _autoStartEnabled = !_autoStartEnabled;
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
            AutoStartToggle.Background = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString("#4caf50")!);
            AutoStartToggleKnob.SetValue(FrameworkElement.HorizontalAlignmentProperty, System.Windows.HorizontalAlignment.Right);
            AutoStartToggleKnob.Margin = new Thickness(0, 0, 2, 0);
        }
        else
        {
            AutoStartToggle.Background = new SolidColorBrush(Colors.LightGray);
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
        _configService.Save();
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
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
            Description = _configService.Settings.Language == "zh" ? "选择工具文件夹" : "Select Tools Folder",
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
                _configService.Settings.Language == "zh" ? "路径不存在" : "Path does not exist",
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
        var msg = _configService.Settings.Language == "zh"
            ? $"扫描完成，共找到 {mergedItems.Count} 个工具"
            : $"Scan complete, found {mergedItems.Count} tool(s)";
        if (pendingCount > 0)
        {
            msg += _configService.Settings.Language == "zh"
                ? $"，其中 {pendingCount} 个需要确认"
                : $", {pendingCount} need confirmation";
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
            var lang = _configService.Settings.Language;
            ToolsPendingWarningText.Text = lang == "zh"
                ? $"有 {pendingCount} 个工具需要确认主程序"
                : $"{pendingCount} tool(s) need main program confirmation";
        }
        else
        {
            ToolsPendingWarning.Visibility = Visibility.Collapsed;
        }
    }
}
