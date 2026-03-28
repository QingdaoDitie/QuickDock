using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using QuickDock.Models;
using QuickDock.Services;

namespace QuickDock.Windows;

public partial class ItemEditWindow : Window
{
    public DockItem? Item { get; private set; }
    private readonly DockItemType[] _types = { DockItemType.Application, DockItemType.Folder, DockItemType.WebPage, DockItemType.Command };
    private bool _initialized;
    private string? _downloadedIconPath;

    public ItemEditWindow() : this(null) { }

    public ItemEditWindow(DockItem? item)
    {
        InitializeComponent();
        
        TypeComboBox.ItemsSource = _types.Select(t => Lang.T($"Type.{t}")).ToList();
        
        if (item != null)
        {
            Item = new DockItem
            {
                Id = item.Id,
                Name = item.Name,
                Type = item.Type,
                Path = item.Path,
                IconPath = item.IconPath,
                Arguments = item.Arguments,
                RunAsAdmin = item.RunAsAdmin
            };
            NameTextBox.Text = item.Name;
            TypeComboBox.SelectedIndex = Array.IndexOf(_types, item.Type);
            PathTextBox.Text = item.Path;
            ArgumentsTextBox.Text = item.Arguments ?? "";
            IconPathTextBox.Text = item.IconPath ?? "";
            RunAsAdminCheckBox.IsChecked = item.RunAsAdmin;
        }
        else
        {
            Item = new DockItem();
            TypeComboBox.SelectedIndex = 0;
        }
        
        _initialized = true;
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Title = Lang.T("Edit.Title");
        NameLabel.Text = Lang.T("Edit.Name") + ":";
        TypeLabel.Text = Lang.T("Edit.Type") + ":";
        PathLabel.Text = Lang.T("Edit.Path") + ":";
        ArgumentsLabel.Text = Lang.T("Edit.Arguments") + ":";
        IconLabel.Text = Lang.T("Edit.IconPath") + ":";
        RunAsAdminCheckBox.Content = Lang.T("RunAsAdmin");
        OkButton.Content = Lang.T("Edit.OK");
        CancelButton.Content = Lang.T("Edit.Cancel");
        FetchIconBtn.Content = Lang.T("Edit.FetchIcon");
        UseIconBtn.Content = Lang.T("Edit.UseIcon");
    }

    private void OnTypeChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (!_initialized || TypeComboBox.SelectedIndex < 0) return;
        
        var type = _types[TypeComboBox.SelectedIndex];
        BrowseButton.Visibility = type == DockItemType.WebPage ? Visibility.Collapsed : Visibility.Visible;
        RunAsAdminCheckBox.Visibility = type == DockItemType.Application ? Visibility.Visible : Visibility.Collapsed;
        
        bool isWebPage = type == DockItemType.WebPage;
        FetchIconBtn.Visibility = isWebPage ? Visibility.Visible : Visibility.Collapsed;
        
        if (!isWebPage)
        {
            IconPreviewArea.Visibility = Visibility.Collapsed;
        }
    }

    private void OnBrowseClick(object sender, RoutedEventArgs e)
    {
        if (TypeComboBox.SelectedIndex < 0) return;
        
        var type = _types[TypeComboBox.SelectedIndex];
        
        if (type == DockItemType.Application)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = Lang.T("Filter.Applications"),
                Title = Lang.T("Select.Application")
            };
            if (dialog.ShowDialog() == true)
            {
                PathTextBox.Text = dialog.FileName;
                if (string.IsNullOrEmpty(NameTextBox.Text))
                {
                    NameTextBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
            }
        }
        else if (type == DockItemType.Folder)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PathTextBox.Text = dialog.SelectedPath;
                if (string.IsNullOrEmpty(NameTextBox.Text))
                {
                    NameTextBox.Text = new DirectoryInfo(dialog.SelectedPath).Name;
                }
            }
        }
    }

    private void OnBrowseIconClick(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = Lang.T("Filter.Icons"),
            Title = Lang.T("Select.Icon")
        };
        if (dialog.ShowDialog() == true)
        {
            var cachedPath = IconCacheService.Instance.CacheLocalIcon(dialog.FileName);
            if (cachedPath != null)
            {
                IconPathTextBox.Text = cachedPath;
                ShowPreviewIcon(cachedPath);
            }
            else
            {
                IconPathTextBox.Text = dialog.FileName;
            }
        }
    }

    private async void OnFetchIconClick(object sender, RoutedEventArgs e)
    {
        var url = PathTextBox.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(url))
        {
            System.Windows.MessageBox.Show(Lang.T("Validation.UrlRequired"), Lang.T("Edit.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!url.StartsWith("http://") && !url.StartsWith("https://"))
        {
            url = "https://" + url;
        }

        IconPreviewArea.Visibility = Visibility.Visible;
        PreviewStatusText.Text = Lang.T("Edit.Fetching");
        PreviewIconImage.Visibility = Visibility.Collapsed;
        UseIconBtn.Visibility = Visibility.Collapsed;
        FetchIconBtn.IsEnabled = false;

        try
        {
            var iconPath = await IconCacheService.Instance.DownloadIconAsync(url);
            
            if (iconPath != null && File.Exists(iconPath))
            {
                _downloadedIconPath = iconPath;
                ShowPreviewIcon(iconPath);
                PreviewStatusText.Text = Lang.T("Edit.FetchSuccess");
                UseIconBtn.Visibility = Visibility.Visible;
            }
            else
            {
                PreviewStatusText.Text = Lang.T("Edit.FetchFailed");
                System.Windows.MessageBox.Show(Lang.T("Edit.FetchFailedHint"), Lang.T("Edit.Title"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception)
        {
            PreviewStatusText.Text = Lang.T("Edit.FetchFailed");
            System.Windows.MessageBox.Show(Lang.T("Edit.FetchFailedHint"), Lang.T("Edit.Title"), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            FetchIconBtn.IsEnabled = true;
        }
    }

    private void ShowPreviewIcon(string iconPath)
    {
        try
        {
            if (!File.Exists(iconPath)) return;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            
            PreviewIconImage.Source = bitmap;
            PreviewIconImage.Visibility = Visibility.Visible;
        }
        catch
        {
            PreviewIconImage.Visibility = Visibility.Collapsed;
        }
    }

    private void OnUseIconClick(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_downloadedIconPath) && File.Exists(_downloadedIconPath))
        {
            IconPathTextBox.Text = _downloadedIconPath;
        }
    }

    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            System.Windows.MessageBox.Show(Lang.T("Validation.NameRequired"), Lang.T("Edit.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(PathTextBox.Text))
        {
            System.Windows.MessageBox.Show(Lang.T("Validation.PathRequired"), Lang.T("Edit.Title"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Item ??= new DockItem();
        Item.Name = NameTextBox.Text.Trim();
        Item.Type = _types[TypeComboBox.SelectedIndex];
        Item.Path = PathTextBox.Text.Trim();
        Item.Arguments = ArgumentsTextBox.Text.Trim();
        Item.IconPath = string.IsNullOrWhiteSpace(IconPathTextBox.Text) ? null : IconPathTextBox.Text.Trim();
        Item.RunAsAdmin = RunAsAdminCheckBox.IsChecked ?? false;

        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
