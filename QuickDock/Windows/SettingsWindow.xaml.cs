using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuickDock.Models;
using QuickDock.Services;

namespace QuickDock.Windows;

public partial class SettingsWindow : Window
{
    private readonly ConfigService _configService;
    private readonly AutoStartService _autoStartService;
    private List<DockItem> _items;
    private bool _initialized;
    private bool _autoStartEnabled;

    public SettingsWindow(ConfigService configService, AutoStartService autoStartService)
    {
        _configService = configService;
        _autoStartService = autoStartService;
        _items = new List<DockItem>(_configService.Items);
        
        InitializeComponent();
        
        ItemsListBox.ItemsSource = _items;
        
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
        
        _initialized = true;
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Title = Lang.T("Settings.Title");
        DockItemsLabel.Text = Lang.T("Settings.DockItems");
        AddButton.Content = Lang.T("Settings.Add");
        EditButton.Content = Lang.T("Settings.Edit");
        DeleteButton.Content = Lang.T("Settings.Delete");
        UpButton.Content = Lang.T("Settings.Up");
        DownButton.Content = Lang.T("Settings.Down");
        LanguageLabel.Text = Lang.T("Settings.Language") + ":";
        OpacityLabel.Text = Lang.T("Settings.Opacity") + ":";
        BackgroundColorLabel.Text = Lang.T("Settings.BackgroundColor") + ":";
        ScaleLabel.Text = Lang.T("Settings.Scale") + ":";
        IconSizeLabel.Text = Lang.T("Settings.IconSize") + ":";
        IconSpacingLabel.Text = Lang.T("Settings.IconSpacing") + ":";
        ShowStatusBarCheckBox.Content = Lang.T("Settings.ShowStatusBar");
        WeatherCityLabel.Text = Lang.T("Settings.WeatherCity") + ":";
        SaveButton.Content = Lang.T("Settings.Save");
        CancelButton.Content = Lang.T("Settings.Cancel");
    }

    private void UpdateAutoStartToggle()
    {
        if (_autoStartEnabled)
        {
            AutoStartToggle.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4a9eff"));
            AutoStartToggleKnob.Background = new SolidColorBrush(System.Windows.Media.Colors.White);
            AutoStartToggleKnob.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            AutoStartToggleKnob.Margin = new Thickness(0, 0, 2, 0);
        }
        else
        {
            AutoStartToggle.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#444"));
            AutoStartToggleKnob.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#888"));
            AutoStartToggleKnob.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            AutoStartToggleKnob.Margin = new Thickness(2, 0, 0, 0);
        }
    }

    private void OnAutoStartClick(object sender, MouseButtonEventArgs e)
    {
        _autoStartEnabled = !_autoStartEnabled;
        
        if (_autoStartEnabled)
        {
            _autoStartService.Enable();
            _configService.Settings.AutoStart = true;
        }
        else
        {
            _autoStartService.Disable();
            _configService.Settings.AutoStart = false;
        }
        
        UpdateAutoStartToggle();
    }

    private void OnItemSelected(object sender, SelectionChangedEventArgs e)
    {
        var hasSelection = ItemsListBox.SelectedIndex >= 0;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
        UpButton.IsEnabled = hasSelection && ItemsListBox.SelectedIndex > 0;
        DownButton.IsEnabled = hasSelection && ItemsListBox.SelectedIndex < _items.Count - 1;
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        var dialog = new ItemEditWindow();
        if (dialog.ShowDialog() == true && dialog.Item != null)
        {
            _items.Add(dialog.Item);
            ItemsListBox.Items.Refresh();
        }
    }

    private void OnEditClick(object sender, RoutedEventArgs e)
    {
        if (ItemsListBox.SelectedItem is DockItem item)
        {
            var dialog = new ItemEditWindow(item);
            if (dialog.ShowDialog() == true && dialog.Item != null)
            {
                var index = _items.IndexOf(item);
                _items[index] = dialog.Item;
                ItemsListBox.Items.Refresh();
            }
        }
    }

    private void OnDeleteClick(object sender, RoutedEventArgs e)
    {
        if (ItemsListBox.SelectedItem is DockItem item)
        {
            var result = System.Windows.MessageBox.Show(
                string.Format(Lang.T("Confirm.Delete"), item.Name), 
                Lang.T("Settings.Title"), 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _items.Remove(item);
                ItemsListBox.Items.Refresh();
            }
        }
    }

    private void OnMoveUpClick(object sender, RoutedEventArgs e)
    {
        var index = ItemsListBox.SelectedIndex;
        if (index > 0)
        {
            (_items[index], _items[index - 1]) = (_items[index - 1], _items[index]);
            ItemsListBox.SelectedIndex = index - 1;
            ItemsListBox.Items.Refresh();
        }
    }

    private void OnMoveDownClick(object sender, RoutedEventArgs e)
    {
        var index = ItemsListBox.SelectedIndex;
        if (index < _items.Count - 1)
        {
            (_items[index], _items[index + 1]) = (_items[index + 1], _items[index]);
            ItemsListBox.SelectedIndex = index + 1;
            ItemsListBox.Items.Refresh();
        }
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized || LanguageComboBox.SelectedIndex < 0) return;
        
        var newLang = LanguageComboBox.SelectedIndex == 0 ? "zh" : "en";
        if (_configService.Settings.Language != newLang)
        {
            _configService.Settings.Language = newLang;
            Lang.CurrentLanguage = newLang == "zh" ? Services.Language.Chinese : Services.Language.English;
            ApplyLanguage();
        }
    }

    private void OnOpacityChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || OpacityValue == null) return;
        
        var value = OpacitySlider.Value;
        _configService.Settings.DockOpacity = value;
        OpacityValue.Text = $"{(int)(value * 100)}%";
    }

    private void OnScaleChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || ScaleValue == null) return;
        
        var value = ScaleSlider.Value;
        _configService.Settings.Scale = value;
        ScaleValue.Text = $"{(int)(value * 100)}%";
    }

    private void OnIconSizeChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || IconSizeValue == null) return;
        
        var value = IconSizeSlider.Value;
        _configService.Settings.IconSize = value;
        IconSizeValue.Text = $"{(int)value}px";
    }

    private void OnIconSpacingChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_initialized || IconSpacingValue == null) return;
        
        var value = IconSpacingSlider.Value;
        _configService.Settings.IconSpacing = value;
        IconSpacingValue.Text = $"{(int)value}px";
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

    private void UpdateBackgroundColorPreview(string colorHex)
    {
        try
        {
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
            BackgroundColorPreview.Background = new SolidColorBrush(color);
        }
        catch
        {
            BackgroundColorPreview.Background = System.Windows.Media.Brushes.Transparent;
        }
    }

    private void OnBackgroundColorChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initialized || BackgroundColorPreview == null) return;
        var colorHex = BackgroundColorTextBox.Text;
        UpdateBackgroundColorPreview(colorHex);
        _configService.Settings.BackgroundColor = colorHex;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _configService.Settings.BackgroundColor = BackgroundColorTextBox.Text;
        
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
}
