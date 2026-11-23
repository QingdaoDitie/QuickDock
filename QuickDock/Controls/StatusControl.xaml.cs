using System.Windows;

namespace QuickDock.Controls;

public partial class StatusControl : System.Windows.Controls.UserControl
{
    public StatusControl() => InitializeComponent();

    internal void ShowWidgets(FrameworkElement first, FrameworkElement second)
    {
        FirstWidget.Content = null;
        SecondWidget.Content = null;
        FirstWidget.Content = first;
        SecondWidget.Content = second;
    }
}
