using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using QuickDock.Models;
using QuickDock.Services;

using WpfBrush = System.Windows.Media.SolidColorBrush;
using WpfThickness = System.Windows.Thickness;
using WpfCornerRadius = System.Windows.CornerRadius;
using WpfFontWeights = System.Windows.FontWeights;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using WpfCursors = System.Windows.Input.Cursors;

namespace QuickDock.Windows;

public partial class ToolsManageWindow : Window
{
    private readonly ConfigService _configService;
    private readonly ToolsScanService _scanService;
    private readonly List<PendingToolFolder> _pendingFolders;

    public ToolsManageWindow(ConfigService configService, ToolsScanService scanService, List<PendingToolFolder>? pendingFolders = null)
    {
        _configService = configService;
        _scanService = scanService;
        _pendingFolders = pendingFolders ?? new List<PendingToolFolder>();
        InitializeComponent();
        
        SaveManageButton.Content = Lang.T("Tools.Save");
        CancelManageButton.Content = Lang.T("Settings.Cancel");
        Title = Lang.T("Settings.ToolsManage");
        HeaderTitleText.Text = Lang.T("Settings.ToolsManage");
        HeaderSubtitleText.Text = Lang.T("Tools.ManageSubtitle");
        
        LoadToolsList();
    }

    private System.Windows.Media.Brush GetBrush(string key)
    {
        return (System.Windows.Media.Brush)FindResource(key);
    }

    private void LoadToolsList()
    {
        ToolsListPanel.Children.Clear();

        foreach (var pending in _pendingFolders)
        {
            CreatePendingFolderItem(pending);
        }

        foreach (var tool in _configService.Settings.ToolsItems)
        {
            CreateConfirmedToolItem(tool);
        }

        if (_pendingFolders.Count == 0 && _configService.Settings.ToolsItems.Count == 0)
        {
            var emptyText = new TextBlock
            {
                Text = Lang.T("Tools.NoExeFound"),
                FontSize = 14,
                Foreground = GetBrush("MutedTextBrush"),
                HorizontalAlignment = WpfHorizontalAlignment.Center,
                Margin = new WpfThickness(0, 40, 0, 0)
            };
            ToolsListPanel.Children.Add(emptyText);
        }
    }

    private void CreatePendingFolderItem(PendingToolFolder pending)
    {
        var border = new System.Windows.Controls.Border
        {
            Background = GetBrush("WarningBackgroundBrush"),
            BorderBrush = GetBrush("WarningBrush"),
            BorderThickness = new WpfThickness(1),
            CornerRadius = new WpfCornerRadius(8),
            Padding = new WpfThickness(12, 8, 12, 8),
            Margin = new WpfThickness(0, 0, 0, 8)
        };

        var stack = new StackPanel();

        var headerPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal
        };

        var nameText = new TextBlock
        {
            Text = pending.FolderName,
            FontSize = 14,
            FontWeight = WpfFontWeights.Medium,
            Foreground = GetBrush("TextBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        headerPanel.Children.Add(nameText);

        var statusText = new TextBlock
        {
            Text = Lang.T("Tools.Pending"),
            FontSize = 11,
            Foreground = GetBrush("WarningBrush"),
            Margin = new WpfThickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        headerPanel.Children.Add(statusText);

        stack.Children.Add(headerPanel);

        var hint = new TextBlock
        {
            Text = Lang.T("Tools.SelectMainExe") + ":",
            FontSize = 12,
            Foreground = GetBrush("MutedTextBrush"),
            Margin = new WpfThickness(0, 6, 0, 4)
        };
        stack.Children.Add(hint);

        var radioButtonPanel = new StackPanel
        {
            Tag = pending
        };

        foreach (var candidate in pending.Candidates)
        {
            var radio = new System.Windows.Controls.RadioButton
            {
                Content = System.IO.Path.GetFileName(candidate),
                GroupName = pending.FolderName,
                Tag = candidate,
                Foreground = GetBrush("TextBrush"),
                Margin = new WpfThickness(0, 2, 0, 2)
            };
            radioButtonPanel.Children.Add(radio);
        }

        stack.Children.Add(radioButtonPanel);

        var confirmButton = new System.Windows.Controls.Button
        {
            Content = Lang.T("Tools.Confirm"),
            Background = GetBrush("SuccessBackgroundBrush"),
            Foreground = GetBrush("SuccessBrush"),
            BorderBrush = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new WpfThickness(0),
            Padding = new WpfThickness(12, 4, 12, 4),
            Cursor = WpfCursors.Hand,
            Margin = new WpfThickness(0, 6, 0, 0),
            Tag = radioButtonPanel
        };
        confirmButton.Click += OnConfirmPendingTool;
        stack.Children.Add(confirmButton);

        border.Child = stack;
        ToolsListPanel.Children.Add(border);
    }

    private void CreateConfirmedToolItem(ToolItem tool)
    {
        var border = new System.Windows.Controls.Border
        {
            Background = GetBrush("SecondaryBackgroundBrush"),
            BorderBrush = GetBrush("BorderBrush"),
            BorderThickness = new WpfThickness(1),
            CornerRadius = new WpfCornerRadius(8),
            Padding = new WpfThickness(12, 8, 12, 8),
            Margin = new WpfThickness(0, 0, 0, 8),
            Tag = tool
        };

        var stack = new StackPanel();

        var headerPanel = new StackPanel
        {
            Orientation = System.Windows.Controls.Orientation.Horizontal
        };

        var nameText = new TextBlock
        {
            Text = tool.DisplayName,
            FontSize = 14,
            FontWeight = WpfFontWeights.Medium,
            Foreground = GetBrush("TextBrush"),
            VerticalAlignment = VerticalAlignment.Center
        };
        headerPanel.Children.Add(nameText);

        var statusText = new TextBlock
        {
            Text = tool.IsConfirmed ? Lang.T("Tools.Confirm") : Lang.T("Tools.Pending"),
            FontSize = 11,
            Foreground = tool.IsConfirmed
                ? GetBrush("SuccessBrush")
                : GetBrush("WarningBrush"),
            Margin = new WpfThickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        headerPanel.Children.Add(statusText);

        var removeButton = new System.Windows.Controls.Button
        {
            Content = Lang.T("Tools.Remove"),
            Background = GetBrush("DangerBackgroundBrush"),
            Foreground = GetBrush("DangerBrush"),
            BorderBrush = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new WpfThickness(0),
            Padding = new WpfThickness(8, 3, 8, 3),
            Cursor = WpfCursors.Hand,
            Margin = new WpfThickness(20, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Tag = tool
        };
        removeButton.Click += OnRemoveTool;
        headerPanel.Children.Add(removeButton);

        var upButton = new System.Windows.Controls.Button
        {
            Content = Lang.T("Settings.Up"),
            Background = GetBrush("WindowPanelBrush"),
            Foreground = GetBrush("TextBrush"),
            BorderBrush = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new WpfThickness(0),
            Padding = new WpfThickness(8, 3, 8, 3),
            Cursor = WpfCursors.Hand,
            Margin = new WpfThickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Tag = tool
        };
        upButton.Click += OnMoveToolUp;
        headerPanel.Children.Add(upButton);

        var downButton = new System.Windows.Controls.Button
        {
            Content = Lang.T("Settings.Down"),
            Background = GetBrush("WindowPanelBrush"),
            Foreground = GetBrush("TextBrush"),
            BorderBrush = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new WpfThickness(0),
            Padding = new WpfThickness(8, 3, 8, 3),
            Cursor = WpfCursors.Hand,
            Margin = new WpfThickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Tag = tool
        };
        downButton.Click += OnMoveToolDown;
        headerPanel.Children.Add(downButton);

        stack.Children.Add(headerPanel);

        var pathText = new TextBlock
        {
            Text = tool.ExePath,
            FontSize = 11,
            Foreground = GetBrush("MutedTextBrush"),
            Margin = new WpfThickness(0, 4, 0, 0)
        };
        stack.Children.Add(pathText);

        border.Child = stack;
        ToolsListPanel.Children.Add(border);
    }

    private void OnConfirmPendingTool(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button) return;
        if (button.Tag is not StackPanel radioPanel) return;
        if (radioPanel.Tag is not PendingToolFolder pending) return;

        string? selectedExe = null;
        foreach (var child in radioPanel.Children)
        {
            if (child is System.Windows.Controls.RadioButton radio && radio.IsChecked == true)
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
            Order = _configService.Settings.ToolsItems.Count
        };

        _configService.Settings.ToolsItems.Add(toolItem);
        NormalizeToolOrder();
        _pendingFolders.Remove(pending);
        LoadToolsList();
    }

    private void OnRemoveTool(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button button && button.Tag is ToolItem tool)
        {
            _configService.Settings.ToolsItems.Remove(tool);
            NormalizeToolOrder();
            LoadToolsList();
        }
    }

    private void OnMoveToolUp(object sender, RoutedEventArgs e)
    {
        MoveTool(sender, -1);
    }

    private void OnMoveToolDown(object sender, RoutedEventArgs e)
    {
        MoveTool(sender, 1);
    }

    private void MoveTool(object sender, int offset)
    {
        if (sender is not System.Windows.Controls.Button button || button.Tag is not ToolItem tool)
        {
            return;
        }

        var tools = _configService.Settings.ToolsItems;
        var index = tools.IndexOf(tool);
        if (index < 0)
        {
            return;
        }

        var targetIndex = index + offset;
        if (targetIndex < 0 || targetIndex >= tools.Count)
        {
            return;
        }

        tools.RemoveAt(index);
        tools.Insert(targetIndex, tool);
        NormalizeToolOrder();
        LoadToolsList();
    }

    private void NormalizeToolOrder()
    {
        for (int i = 0; i < _configService.Settings.ToolsItems.Count; i++)
        {
            _configService.Settings.ToolsItems[i].Order = i;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
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
