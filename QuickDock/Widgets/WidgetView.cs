using System.Windows;
using System.Windows.Controls;

namespace QuickDock.Widgets;

internal static class WidgetView
{
    public static TextBlock Text(double size = 12, bool subdued = false)
    {
        var text = new TextBlock { FontSize = size, VerticalAlignment = VerticalAlignment.Center };
        text.SetResourceReference(TextBlock.ForegroundProperty, subdued ? "MutedTextBrush" : "TextBrush");
        return text;
    }
}
