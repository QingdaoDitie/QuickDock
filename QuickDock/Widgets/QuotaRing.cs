using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfBrush = System.Windows.Media.Brush;
using WpfColor = System.Windows.Media.Color;
using WpfPath = System.Windows.Shapes.Path;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;

namespace QuickDock.Widgets;

internal sealed class QuotaRing
{
    private const double Center = 18;
    private const double OuterRadius = 15.5;
    private const double InnerRadius = 12;
    private static readonly WpfBrush TrackBrush = FrozenBrush(55, 255, 255, 255);
    public static WpfBrush CodexBrush { get; } = FrozenBrush(255, 116, 183, 255);
    public static WpfBrush ClaudeBrush { get; } = FrozenBrush(255, 247, 174, 132);

    private readonly Grid _dial = new() { Width = Center * 2, Height = Center * 2 };
    private readonly WpfPath _outerTrack = CreateTrack(OuterRadius);
    private readonly WpfPath _innerTrack = CreateTrack(InnerRadius);
    private readonly WpfPath _outerProgress = CreatePath();
    private readonly WpfPath _innerProgress = CreatePath();
    private readonly StackPanel _values = new() { VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _outerValue = CreateValue();
    private readonly TextBlock _innerValue = CreateValue();
    private readonly TextBlock _caption = WidgetView.Text(9, subdued: true);
    private readonly string _defaultLabel;

    public FrameworkElement View { get; }

    public QuotaRing(string defaultLabel)
    {
        _defaultLabel = defaultLabel;
        _caption.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
        _caption.Margin = new Thickness(0, 1, 0, 0);
        _values.Children.Add(_outerValue);
        _values.Children.Add(_innerValue);
        _dial.Children.Add(_outerTrack);
        _dial.Children.Add(_innerTrack);
        _dial.Children.Add(_outerProgress);
        _dial.Children.Add(_innerProgress);
        _dial.Children.Add(_values);
        var column = new StackPanel();
        column.Children.Add(_dial);
        column.Children.Add(_caption);
        View = column;
    }

    public void Update(QuotaWindow? codex, QuotaWindow? claude)
    {
        var outer = codex ?? claude;
        var overlay = codex != null && claude != null;
        var outerBrush = codex != null ? CodexBrush : ClaudeBrush;
        _outerProgress.Stroke = outerBrush;
        _outerProgress.Data = CreateProgress(outer?.Remaining ?? 0, OuterRadius);
        _outerValue.Text = outer == null ? "–" : $"{outer.Remaining:0}%";
        _outerValue.Foreground = outer == null ? TrackBrush : outerBrush;
        _outerValue.FontSize = overlay ? 8.5 : 10;
        _innerTrack.Visibility = _innerProgress.Visibility = _innerValue.Visibility =
            overlay ? Visibility.Visible : Visibility.Collapsed;
        _innerProgress.Stroke = ClaudeBrush;
        _innerProgress.Data = CreateProgress(overlay ? claude!.Remaining : 0, InnerRadius);
        _innerValue.Text = overlay ? $"{claude!.Remaining:0}%" : string.Empty;
        _innerValue.Foreground = ClaudeBrush;
        _caption.Text = overlay && codex!.Label != claude!.Label
            ? $"{codex.Label}/{claude.Label}"
            : outer?.Label ?? _defaultLabel;
    }

    private static Geometry CreateProgress(double remaining, double radius)
    {
        if (remaining <= 0) return Geometry.Empty;
        if (remaining >= 100) return CreateCircle(radius);

        var angle = remaining / 100 * Math.Tau;
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(new WpfPoint(Center, Center - radius), isFilled: false, isClosed: false);
            var end = new WpfPoint(Center + radius * Math.Sin(angle), Center - radius * Math.Cos(angle));
            context.ArcTo(end, new WpfSize(radius, radius), 0, remaining > 50, SweepDirection.Clockwise, true, false);
        }
        geometry.Freeze();
        return geometry;
    }

    private static EllipseGeometry CreateCircle(double radius) => new(new WpfPoint(Center, Center), radius, radius);

    private static WpfPath CreateTrack(double radius)
    {
        var path = CreatePath();
        path.Data = CreateCircle(radius);
        path.Stroke = TrackBrush;
        return path;
    }

    private static WpfPath CreatePath() => new()
    {
        StrokeThickness = 2.3,
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round
    };

    private static TextBlock CreateValue() => new()
    {
        HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
        FontWeight = FontWeights.SemiBold,
        FontSize = 8.5,
        LineHeight = 9,
        LineStackingStrategy = LineStackingStrategy.BlockLineHeight
    };

    private static WpfBrush FrozenBrush(byte alpha, byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(WpfColor.FromArgb(alpha, red, green, blue));
        brush.Freeze();
        return brush;
    }
}
