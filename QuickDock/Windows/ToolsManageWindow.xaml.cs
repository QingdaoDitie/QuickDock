using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuickDock.Models;
using QuickDock.Services;

using WpfBrush = System.Windows.Media.Brush;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfRadioButton = System.Windows.Controls.RadioButton;
using WpfListBox = System.Windows.Controls.ListBox;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfCursors = System.Windows.Input.Cursors;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfPoint = System.Windows.Point;
using WpfCornerRadius = System.Windows.CornerRadius;
using WpfThickness = System.Windows.Thickness;
using WpfFontWeights = System.Windows.FontWeights;

namespace QuickDock.Windows;

public partial class ToolsManageWindow : Window
{
    private readonly ConfigService _configService;
    private readonly ToolsScanService _scanService;
    private readonly List<PendingToolFolder> _pendingFolders;
    private readonly List<ToolItem> _tools;

    private bool _isDragging;
    private ToolItem? _dragToolItem;
    private WpfPoint _dragStartPoint;

    public ToolsManageWindow(ConfigService configService, ToolsScanService scanService, List<PendingToolFolder>? pendingFolders = null)
    {
        _configService = configService;
        _scanService = scanService;
        _pendingFolders = pendingFolders ?? new List<PendingToolFolder>();
        _tools = new List<ToolItem>(_configService.Settings.ToolsItems);

        InitializeComponent();

        SaveManageButton.Content = Lang.T("Tools.Save");
        CancelManageButton.Content = Lang.T("Settings.Cancel");
        Title = Lang.T("Settings.ToolsManage");
        HeaderTitleText.Text = Lang.T("Settings.ToolsManage");
        HeaderSubtitleText.Text = Lang.T("Tools.ManageSubtitle");

        LoadPendingItems();
        RefreshToolsList();
    }

    private void LoadPendingItems()
    {
        PendingPanel.Children.Clear();

        if (_pendingFolders.Count == 0)
        {
            PendingPanel.Visibility = Visibility.Collapsed;
            return;
        }

        PendingPanel.Visibility = Visibility.Visible;

        foreach (var pending in _pendingFolders)
        {
            var border = new Border
            {
                Background = (WpfBrush)FindResource("WarningBackgroundBrush"),
                BorderBrush = (WpfBrush)FindResource("WarningBrush"),
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
                Foreground = (WpfBrush)FindResource("TextBrush"),
                VerticalAlignment = VerticalAlignment.Center
            });
            header.Children.Add(new TextBlock
            {
                Text = Lang.T("Tools.Pending"),
                FontSize = 10,
                Foreground = (WpfBrush)FindResource("WarningBrush"),
                Margin = new WpfThickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            stack.Children.Add(header);

            stack.Children.Add(new TextBlock
            {
                Text = Lang.T("Tools.SelectMainExe") + ":",
                FontSize = 11,
                Foreground = (WpfBrush)FindResource("MutedTextBrush"),
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
                    Foreground = (WpfBrush)FindResource("TextBrush"),
                    Margin = new WpfThickness(0, 1, 0, 1)
                });
            }
            stack.Children.Add(radioPanel);

            var confirmBtn = new WpfButton
            {
                Content = Lang.T("Tools.Confirm"),
                Style = (Style)FindResource("SuccessButtonStyle"),
                Padding = new WpfThickness(10, 3, 10, 3),
                Cursor = WpfCursors.Hand,
                Margin = new WpfThickness(0, 4, 0, 0),
                Tag = radioPanel
            };
            confirmBtn.Click += OnConfirmPendingTool;
            stack.Children.Add(confirmBtn);

            border.Child = stack;
            PendingPanel.Children.Add(border);
        }
    }

    private void RefreshToolsList()
    {
        ToolsListBox.ItemsSource = null;
        ToolsListBox.ItemsSource = _tools;
    }

    private void OnConfirmPendingTool(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton button) return;
        if (button.Tag is not StackPanel radioPanel) return;
        if (radioPanel.Tag is not PendingToolFolder pending) return;

        string? selectedExe = null;
        foreach (var child in radioPanel.Children)
        {
            if (child is WpfRadioButton radio && radio.IsChecked == true)
            {
                selectedExe = radio.Tag as string;
                break;
            }
        }

        if (string.IsNullOrEmpty(selectedExe)) return;

        var toolItem = new ToolItem
        {
            DisplayName = System.IO.Path.GetFileNameWithoutExtension(selectedExe),
            ExePath = selectedExe,
            SourceFolder = pending.FolderPath,
            IsConfirmed = true,
            Order = _tools.Count
        };

        _tools.Add(toolItem);
        NormalizeToolOrder();
        _pendingFolders.Remove(pending);
        LoadPendingItems();
        RefreshToolsList();
    }

    private void OnRemoveToolClick(object sender, RoutedEventArgs e)
    {
        if (sender is not WpfButton button || button.Tag is not ToolItem tool) return;
        _tools.Remove(tool);
        NormalizeToolOrder();
        RefreshToolsList();
    }

    private void OnToolDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging) return;
        if (ToolsListBox.SelectedItem is not ToolItem tool) return;

        var input = new WpfTextBox
        {
            Text = tool.DisplayName,
            Style = (Style)FindResource("TextBoxStyle"),
            Margin = new WpfThickness(0, 0, 0, 8)
        };

        var pathInput = new WpfTextBox
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
            Style = (Style)FindResource("WindowStyle")
        };

        var panel = new StackPanel { Margin = new WpfThickness(16) };
        panel.Children.Add(new TextBlock
        {
            Text = Lang.T("Settings.Name"),
            FontSize = 12,
            Foreground = (WpfBrush)FindResource("MutedTextBrush"),
            Margin = new WpfThickness(0, 0, 0, 4)
        });
        panel.Children.Add(input);
        panel.Children.Add(new TextBlock
        {
            Text = Lang.T("Settings.Path"),
            FontSize = 12,
            Foreground = (WpfBrush)FindResource("MutedTextBrush"),
            Margin = new WpfThickness(0, 0, 0, 4)
        });
        panel.Children.Add(pathInput);

        var btnPanel = new StackPanel
        {
            Orientation = WpfOrientation.Horizontal,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
            Margin = new WpfThickness(0, 12, 0, 0)
        };

        var okBtn = new WpfButton
        {
            Content = Lang.T("Settings.Save"),
            Style = (Style)FindResource("PrimaryActionButtonStyle"),
            Padding = new WpfThickness(20, 5, 20, 5),
            Margin = new WpfThickness(0, 0, 8, 0)
        };
        okBtn.Click += (_, _) =>
        {
            tool.DisplayName = input.Text.Trim();
            tool.ExePath = pathInput.Text.Trim();
            RefreshToolsList();
            dialog.DialogResult = true;
        };

        var cancelBtn = new WpfButton
        {
            Content = Lang.T("Settings.Cancel"),
            Style = (Style)FindResource("SettingsButtonStyle"),
            Padding = new WpfThickness(20, 5, 20, 5)
        };
        cancelBtn.Click += (_, _) => { dialog.DialogResult = false; };

        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        panel.Children.Add(btnPanel);

        dialog.Content = panel;
        dialog.ShowDialog();
    }

    private void OnToolDragStart(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is WpfButton) return;
        _dragStartPoint = e.GetPosition(null);
        _dragToolItem = null;
        _isDragging = false;

        if (ToolsListBox.ContainerFromElement((DependencyObject)e.OriginalSource) is ListBoxItem lbi && lbi.Content is ToolItem tool)
        {
            _dragToolItem = tool;
        }
    }

    private void OnToolDragging(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragToolItem == null || _isDragging) return;
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var pos = e.GetPosition(null);
        var diff = _dragStartPoint - pos;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _isDragging = true;
        var index = _tools.IndexOf(_dragToolItem);
        if (index < 0) return;

        var effect = DragDrop.DoDragDrop(ToolsListBox, _dragToolItem, WpfDragDropEffects.Move);
        if (effect == WpfDragDropEffects.Move)
        {
            RefreshToolsList();
            if (index < _tools.Count)
                ToolsListBox.SelectedIndex = index;
        }
        _isDragging = false;
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

        var oldIndex = _tools.IndexOf(dropItem);
        var newIndex = _tools.IndexOf(targetItem);
        if (oldIndex < 0 || newIndex < 0) return;

        _tools.RemoveAt(oldIndex);
        _tools.Insert(newIndex, dropItem);
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
        for (int i = 0; i < _tools.Count; i++)
        {
            if (listBox.ItemContainerGenerator.ContainerFromIndex(i) is ListBoxItem lbi)
            {
                var rect = VisualTreeHelper.GetDescendantBounds(lbi);
                var pos = listBox.TranslatePoint(position, lbi);
                if (rect.Contains(pos))
                    return _tools[i];
            }
        }
        return null;
    }

    private void NormalizeToolOrder()
    {
        for (int i = 0; i < _tools.Count; i++)
            _tools[i].Order = i;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        _configService.Settings.ToolsItems = _tools;
        NormalizeToolOrder();
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
