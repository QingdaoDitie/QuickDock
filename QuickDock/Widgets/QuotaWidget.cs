using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Threading;
using QuickDock.Services;
using QuickDock.WidgetContracts;
using WpfButton = System.Windows.Controls.Button;
using WpfBrush = System.Windows.Media.Brush;
using WpfOrientation = System.Windows.Controls.Orientation;

namespace QuickDock.Widgets;

internal sealed class QuotaWidget : IDockWidget
{
    private readonly DispatcherTimer _codexTimer = new() { Interval = TimeSpan.FromMinutes(5) };
    private readonly DispatcherTimer _codexRetryTimer = new() { Interval = TimeSpan.FromSeconds(20) };
    private readonly DispatcherTimer _claudeTimer = new() { Interval = TimeSpan.FromSeconds(15) };
    private readonly DispatcherTimer _claudeRetryTimer = new() { Interval = TimeSpan.FromSeconds(3) };
    private const int RetryLimit = 3;
    private int _codexRetries;
    private int _claudeRetries;
    private readonly QuotaRing _shortRing = new("5h");
    private readonly QuotaRing _weeklyRing = new("7d");
    private readonly StackPanel _rings = new() { Orientation = WpfOrientation.Horizontal };
    private readonly FrameworkElement _codexLegend = CreateLegend("Codex", QuotaRing.CodexBrush);
    private readonly FrameworkElement _claudeLegend = CreateLegend("Claude", QuotaRing.ClaudeBrush);
    private readonly TextBlock _status = WidgetView.Text(11, subdued: true);
    private readonly WpfButton _button;
    private CancellationTokenSource? _active;
    private bool _readingCodex;
    private bool _readingClaude;
    private bool _codexReadCompleted;
    private bool _claudeReadCompleted;
    private IReadOnlyList<QuotaWindow> _codex = [];
    private IReadOnlyList<QuotaWindow> _claude = [];

    public FrameworkElement View => _button;

    public QuotaWidget()
    {
        _weeklyRing.View.Margin = new Thickness(5, 0, 0, 0);
        _rings.Children.Add(_shortRing.View);
        _rings.Children.Add(_weeklyRing.View);
        var legend = new StackPanel { Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        legend.Children.Add(_codexLegend);
        legend.Children.Add(_claudeLegend);
        _rings.Children.Add(legend);
        var content = new Grid();
        content.Children.Add(_status);
        content.Children.Add(_rings);
        _button = new WpfButton
        {
            Content = content,
            Background = System.Windows.Media.Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(2, 0, 2, 0)
        };
        _button.SetResourceReference(FrameworkElement.StyleProperty, "SettingsButtonStyle");
        _button.Click += (_, _) => RefreshBoth();
        _codexTimer.Tick += (_, _) =>
        {
            if (!_codexRetryTimer.IsEnabled) _ = RefreshCodexAsync(startNewCycle: true);
        };
        _codexRetryTimer.Tick += (_, _) => { _codexRetryTimer.Stop(); _ = RefreshCodexAsync(); };
        _claudeTimer.Tick += (_, _) =>
        {
            if (!_claudeRetryTimer.IsEnabled) _ = RefreshClaudeAsync(startNewCycle: true);
        };
        _claudeRetryTimer.Tick += (_, _) => { _claudeRetryTimer.Stop(); _ = RefreshClaudeAsync(); };
        UpdateLanguage();
    }

    public void SetActive(bool active)
    {
        if ((_active != null) == active) return;
        if (active)
        {
            _active = new CancellationTokenSource();
            _codexTimer.Start();
            _claudeTimer.Start();
            RefreshBoth();
            return;
        }

        _codexTimer.Stop();
        _codexRetryTimer.Stop();
        _claudeTimer.Stop();
        _claudeRetryTimer.Stop();
        _active!.Cancel();
        _active.Dispose();
        _active = null;
        _readingCodex = _readingClaude = false;
        _codexReadCompleted = _claudeReadCompleted = false;
        _codex = _claude = [];
        UpdateLanguage();
    }

    private void RefreshBoth()
    {
        _ = RefreshCodexAsync(startNewCycle: true);
        _ = RefreshClaudeAsync(startNewCycle: true);
    }

    private async Task RefreshCodexAsync(bool startNewCycle = false)
    {
        if (_active == null || _readingCodex) return;
        if (startNewCycle)
        {
            _codexRetryTimer.Stop();
            _codexRetries = 0;
        }
        var token = _active.Token;
        _readingCodex = true;
        var windows = await ReadAsync(QuotaSources.ReadCodexAsync, token);
        if (token.IsCancellationRequested) return;
        _readingCodex = false;
        _codexReadCompleted = true;
        _codex = windows;
        ScheduleRetry(_codexRetryTimer, ref _codexRetries, windows.Count > 0);
        UpdateLanguage();
    }

    private async Task RefreshClaudeAsync(bool startNewCycle = false)
    {
        if (_active == null || _readingClaude) return;
        if (startNewCycle)
        {
            _claudeRetryTimer.Stop();
            _claudeRetries = 0;
        }
        var token = _active.Token;
        _readingClaude = true;
        var windows = await ReadAsync(QuotaSources.ReadClaudeAsync, token);
        if (token.IsCancellationRequested) return;
        _claude = windows;
        _readingClaude = false;
        _claudeReadCompleted = true;
        ScheduleRetry(_claudeRetryTimer, ref _claudeRetries, windows.Count > 0);
        UpdateLanguage();
    }

    private static void ScheduleRetry(DispatcherTimer timer, ref int retries, bool succeeded)
    {
        if (succeeded)
        {
            timer.Stop();
            return;
        }
        if (retries >= RetryLimit) return;

        retries++;
        timer.Start();
    }

    private static async Task<IReadOnlyList<QuotaWindow>> ReadAsync(
        Func<CancellationToken, Task<IReadOnlyList<QuotaWindow>>> read, CancellationToken token)
    {
        try { return await Task.Run(() => read(token), token); }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException) Debug.WriteLine($"Quota read failed: {ex.Message}");
            return [];
        }
    }

    public void UpdateLanguage()
    {
        var hasCodex = _codex.Count > 0;
        var hasClaude = _claude.Count > 0;
        var hasQuota = hasCodex || hasClaude;
        _status.Visibility = hasQuota ? Visibility.Collapsed : Visibility.Visible;
        _rings.Visibility = hasQuota ? Visibility.Visible : Visibility.Collapsed;
        _codexLegend.Visibility = hasCodex ? Visibility.Visible : Visibility.Collapsed;
        _claudeLegend.Visibility = hasClaude ? Visibility.Visible : Visibility.Collapsed;
        _status.Text = Lang.T(_codexReadCompleted && _claudeReadCompleted
            ? "Widget.QuotaUnavailable" : "Widget.QuotaLoading");
        UpdateRing(_shortRing, QuotaPeriod.Short);
        UpdateRing(_weeklyRing, QuotaPeriod.Weekly);

        var details = _codex.Select(window => Detail("Codex", window))
            .Concat(_claude.Select(window => Detail("Claude", window))).ToList();
        if (!hasQuota) details.Add(_status.Text);
        var refresh = Lang.T("Widget.QuotaRefresh");
        details.Add(refresh);
        _button.ToolTip = string.Join(Environment.NewLine, details);
        System.Windows.Automation.AutomationProperties.SetName(_button, refresh);
    }

    private void UpdateRing(QuotaRing ring, QuotaPeriod period) =>
        ring.Update(_codex.FirstOrDefault(window => window.Period == period),
            _claude.FirstOrDefault(window => window.Period == period));

    private static FrameworkElement CreateLegend(string provider, WpfBrush color)
    {
        var key = new StackPanel { Orientation = WpfOrientation.Horizontal, Margin = new Thickness(0, 1, 0, 1) };
        key.Children.Add(new Ellipse
        {
            Width = 5, Height = 5, Fill = color, Margin = new Thickness(0, 0, 4, 0),
            VerticalAlignment = VerticalAlignment.Center
        });
        var label = WidgetView.Text(10);
        label.Text = provider;
        key.Children.Add(label);
        return key;
    }

    private static string Detail(string provider, QuotaWindow window)
    {
        var detail = $"{provider} {window.Label} · {window.Remaining:0.#}% {Lang.T("Widget.QuotaRemaining")}";
        return window.ResetsAt is { } reset ? $"{detail} · {Lang.T("Widget.QuotaReset")} {reset.ToLocalTime():g}" : detail;
    }

    public void Dispose() => SetActive(false);
}
