using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuickDock.Controls;

public partial class StatusControl : System.Windows.Controls.UserControl
{
    public static readonly DependencyProperty StatusServiceProperty =
        DependencyProperty.Register(
            nameof(StatusService),
            typeof(Services.StatusService),
            typeof(StatusControl),
            new PropertyMetadata(null, OnStatusServiceChanged));

    public Services.StatusService? StatusService
    {
        get => (Services.StatusService?)GetValue(StatusServiceProperty);
        set => SetValue(StatusServiceProperty, value);
    }

    public StatusControl()
    {
        InitializeComponent();
    }

    private static void OnStatusServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusControl control && e.NewValue is Services.StatusService service)
        {
            control.DataContext = service;
        }
    }

    private void OnRefreshClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        StatusService?.RequestRefresh();
    }
}
