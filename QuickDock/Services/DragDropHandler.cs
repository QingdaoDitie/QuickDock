using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using QuickDock.Controls;
using QuickDock.Models;

using WpfDragEventArgs = System.Windows.DragEventArgs;
using WpfDragDropEffects = System.Windows.DragDropEffects;
using WpfIDataObject = System.Windows.IDataObject;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfPoint = System.Windows.Point;

namespace QuickDock.Services;

public class DragDropHandler
{
    private readonly Window _window;
    private readonly ConfigService _configService;
    private readonly ObservableCollection<DockItem> _dockItems;
    private readonly ObservableCollection<ToolItem> _toolItems;
    private readonly ItemsControl _dockItemsControl;
    private readonly ItemsControl _toolsItemsControl;
    private readonly Func<bool> _isAnimatingFunc;
    private readonly Func<bool> _isHiddenFunc;
    private readonly Action _refreshToolsAction;

    private WpfPoint _dragStartPoint;
    private bool _isInternalDrag;
    private DateTime _autoHideSuppressedUntil = DateTime.MinValue;

    public const string DockItemDragFormat = "QuickDock.DockItem";
    public const string ToolItemDragFormat = "QuickDock.ToolItem";

    public bool IsInternalDrag => _isInternalDrag;
    public bool IsAutoHideSuppressed => _isInternalDrag || DateTime.Now < _autoHideSuppressedUntil;

    public event Action<bool>? AutoHideSuppressionChanged;

    public DragDropHandler(
        Window window,
        ConfigService configService,
        ObservableCollection<DockItem> dockItems,
        ObservableCollection<ToolItem> toolItems,
        ItemsControl dockItemsControl,
        ItemsControl toolsItemsControl,
        Func<bool> isAnimatingFunc,
        Func<bool> isHiddenFunc,
        Action refreshToolsAction)
    {
        _window = window;
        _configService = configService;
        _dockItems = dockItems;
        _toolItems = toolItems;
        _dockItemsControl = dockItemsControl;
        _toolsItemsControl = toolsItemsControl;
        _isAnimatingFunc = isAnimatingFunc;
        _isHiddenFunc = isHiddenFunc;
        _refreshToolsAction = refreshToolsAction;
    }

    public void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(_window);
    }

    public void OnPreviewMouseMove(WpfMouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _isAnimatingFunc() || _isInternalDrag)
        {
            return;
        }

        var currentPosition = e.GetPosition(_window);
        if (Math.Abs(currentPosition.X - _dragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(currentPosition.Y - _dragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var dockControl = FindAncestor<DockItemControl>(e.OriginalSource as DependencyObject);
        if (dockControl?.DataContext is DockItem dockItem)
        {
            StartInternalDrag(new System.Windows.DataObject(DockItemDragFormat, dockItem));
            return;
        }

        if (dockControl?.DataContext is ToolItem toolItem)
        {
            StartInternalDrag(new System.Windows.DataObject(ToolItemDragFormat, toolItem));
        }
    }

    public void OnDragEnter(WpfDragEventArgs e)
    {
        HandleDragEvent(e);
    }

    public void OnDragOver(WpfDragEventArgs e)
    {
        HandleDragEvent(e);
    }

    public void OnDrop(WpfDragEventArgs e)
    {
        if (TryHandleInternalDrop(e))
        {
            return;
        }

        if (!e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop)) return;

        var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
        if (files == null) return;

        foreach (var file in files)
        {
            var ext = System.IO.Path.GetExtension(file).ToLower();
            if (ext != ".exe" && ext != ".lnk") continue;

            string targetPath = file;
            string name = System.IO.Path.GetFileNameWithoutExtension(file);

            if (ext == ".lnk")
            {
                var resolved = ShortcutResolver.Resolve(file);
                if (resolved != null)
                {
                    targetPath = resolved;
                    name = System.IO.Path.GetFileNameWithoutExtension(resolved);
                }
            }

            var exists = _configService.Items.Any(i =>
                i.Path.Equals(targetPath, StringComparison.OrdinalIgnoreCase));
            if (exists) continue;

            var item = new DockItem
            {
                Name = name,
                Type = DockItemType.Application,
                Path = targetPath
            };
            _configService.Items.Add(item);
            _dockItems.Add(item);
        }
        _configService.Save();
    }

    public void OnDockItemsDragOver(WpfDragEventArgs e)
    {
        if (e.Data.GetDataPresent(DockItemDragFormat))
        {
            e.Effects = WpfDragDropEffects.Move;
            e.Handled = true;
            return;
        }

        HandleDragEvent(e);
    }

    public void OnDockItemsDrop(WpfDragEventArgs e)
    {
        if (e.Data.GetData(DockItemDragFormat) is not DockItem draggedItem)
        {
            OnDrop(e);
            return;
        }

        var targetIndex = GetDropIndex(_dockItemsControl, e.GetPosition(_dockItemsControl), _dockItems.Count);
        MoveDockItem(draggedItem, targetIndex);
        e.Handled = true;
    }

    public void OnToolsItemsDragOver(WpfDragEventArgs e)
    {
        if (e.Data.GetDataPresent(ToolItemDragFormat))
        {
            e.Effects = WpfDragDropEffects.Move;
        }
        else
        {
            e.Effects = WpfDragDropEffects.None;
        }

        e.Handled = true;
    }

    public void OnToolsItemsDrop(WpfDragEventArgs e)
    {
        if (e.Data.GetData(ToolItemDragFormat) is not ToolItem draggedItem)
        {
            return;
        }

        var targetIndex = GetDropIndex(_toolsItemsControl, e.GetPosition(_toolsItemsControl), _toolItems.Count);
        MoveToolItem(draggedItem, targetIndex);
        e.Handled = true;
    }

    private void StartInternalDrag(WpfIDataObject data)
    {
        try
        {
            _isInternalDrag = true;
            _autoHideSuppressedUntil = DateTime.MaxValue;
            AutoHideSuppressionChanged?.Invoke(true);
            DragDrop.DoDragDrop(_window, data, WpfDragDropEffects.Move);
        }
        finally
        {
            _isInternalDrag = false;
            _autoHideSuppressedUntil = DateTime.Now.AddMilliseconds(350);
            AutoHideSuppressionChanged?.Invoke(false);
        }
    }

    private void HandleDragEvent(WpfDragEventArgs e)
    {
        if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(System.Windows.DataFormats.FileDrop);
            if (files != null)
            {
                foreach (var file in files)
                {
                    var ext = System.IO.Path.GetExtension(file).ToLower();
                    if (ext == ".exe" || ext == ".lnk")
                    {
                        e.Effects = System.Windows.DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }
        }
        e.Effects = WpfDragDropEffects.None;
        e.Handled = true;
    }

    private bool TryHandleInternalDrop(WpfDragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DockItemDragFormat) && !e.Data.GetDataPresent(ToolItemDragFormat))
        {
            return false;
        }

        e.Effects = WpfDragDropEffects.Move;
        e.Handled = true;
        return true;
    }

    private int GetDropIndex(ItemsControl itemsControl, WpfPoint position, int itemCount)
    {
        if (itemCount == 0)
        {
            return 0;
        }

        for (int i = 0; i < itemCount; i++)
        {
            if (itemsControl.ItemContainerGenerator.ContainerFromIndex(i) is not FrameworkElement container)
            {
                continue;
            }

            var topLeft = container.TranslatePoint(new WpfPoint(0, 0), itemsControl);
            var midpoint = topLeft.X + container.ActualWidth / 2;
            if (position.X < midpoint)
            {
                return i;
            }
        }

        return itemCount;
    }

    private void MoveDockItem(DockItem draggedItem, int targetIndex)
    {
        var sourceIndex = _dockItems.IndexOf(draggedItem);
        if (sourceIndex < 0)
        {
            return;
        }

        if (targetIndex > sourceIndex)
        {
            targetIndex--;
        }

        targetIndex = Math.Clamp(targetIndex, 0, Math.Max(0, _dockItems.Count - 1));
        if (sourceIndex == targetIndex)
        {
            return;
        }

        _dockItems.Move(sourceIndex, targetIndex);
        _configService.Items.Clear();
        foreach (var item in _dockItems)
        {
            _configService.Items.Add(item);
        }
        _configService.Save();
    }

    private void MoveToolItem(ToolItem draggedItem, int targetIndex)
    {
        var sourceIndex = _toolItems.IndexOf(draggedItem);
        if (sourceIndex < 0)
        {
            return;
        }

        if (targetIndex > sourceIndex)
        {
            targetIndex--;
        }

        targetIndex = Math.Clamp(targetIndex, 0, Math.Max(0, _toolItems.Count - 1));
        if (sourceIndex == targetIndex)
        {
            return;
        }

        _toolItems.Move(sourceIndex, targetIndex);
        for (int i = 0; i < _toolItems.Count; i++)
        {
            _toolItems[i].Order = i;
        }

        var pendingTools = _configService.Settings.ToolsItems
            .Where(t => !t.IsConfirmed)
            .ToList();
        _configService.Settings.ToolsItems = _toolItems.Concat(pendingTools).ToList();
        _configService.Save();
        _refreshToolsAction();
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
            {
                return match;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }
}
