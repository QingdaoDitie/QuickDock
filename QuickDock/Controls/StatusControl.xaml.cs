using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using QuickDock.Services;

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
        ApplyLanguage();
    }

    private static void OnStatusServiceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is StatusControl control && e.NewValue is Services.StatusService service)
        {
            control.DataContext = service;
            control.ApplyLanguage();
        }
    }

    private void OnRefreshClick(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        StatusService?.RequestRefresh();
    }

    private void ApplyLanguage()
    {
        WeatherIconText.ToolTip = Lang.T("Status.RefreshWeather");
        CpuLabelText.Text = "CPU";
        MemLabelText.Text = "MEM";
        GpuLabelText.Text = "GPU";
    }
}
