using System.Collections;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GitChurnCalculator.UI.Models;

namespace GitChurnCalculator.UI.Controls;

public sealed class TimeSeriesChart : Control
{
    public static readonly StyledProperty<IEnumerable?> SeriesProperty =
        AvaloniaProperty.Register<TimeSeriesChart, IEnumerable?>(nameof(Series));

    private static readonly Color[] Palette =
    [
        Color.Parse("#4e79a7"),
        Color.Parse("#f28e2b"),
        Color.Parse("#e15759"),
        Color.Parse("#76b7b2"),
        Color.Parse("#59a14f"),
        Color.Parse("#edc949"),
        Color.Parse("#af7aa1"),
        Color.Parse("#ff9da7"),
        Color.Parse("#9c755f"),
        Color.Parse("#bab0ab"),
    ];

    public IEnumerable? Series
    {
        get => GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SeriesProperty)
            InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var series = ReadSeries();
        if (series.Count == 0)
        {
            DrawEmptyState(context);
            return;
        }

        var margin = new Thickness(64, 24, 260, 52);
        var plot = new Rect(
            margin.Left,
            margin.Top,
            Math.Max(1, Bounds.Width - margin.Left - margin.Right),
            Math.Max(1, Bounds.Height - margin.Top - margin.Bottom));

        var allPoints = series.SelectMany(item => item.Points).ToArray();
        var minDate = allPoints.Min(point => point.Date);
        var maxDate = allPoints.Max(point => point.Date);
        var maxScore = Math.Max(1, allPoints.Max(point => point.ChurnRiskScore));

        DrawGridAndAxes(context, plot, minDate, maxDate, maxScore);
        DrawSeries(context, series, plot, minDate, maxDate, maxScore);
        DrawLegend(context, series, plot);
    }

    private List<TimeSeriesGraphSeries> ReadSeries() =>
        Series?.OfType<TimeSeriesGraphSeries>().ToList() ?? [];

    private void DrawEmptyState(DrawingContext context)
    {
        var text = CreateText("Run a time series analysis to display the churn graph.", 14, Brushes.Gray);
        context.DrawText(text, new Point(16, 16));
    }

    private static void DrawGridAndAxes(
        DrawingContext context,
        Rect plot,
        DateTime minDate,
        DateTime maxDate,
        double maxScore)
    {
        var axisPen = new Pen(Brushes.Gray, 1);
        var gridPen = new Pen(new SolidColorBrush(Color.FromRgb(225, 225, 225)), 1);

        for (var i = 0; i <= 4; i++)
        {
            var ratio = i / 4.0;
            var y = plot.Bottom - ratio * plot.Height;
            context.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            var label = CreateText((maxScore * ratio).ToString("0.##", CultureInfo.InvariantCulture), 11, Brushes.Gray);
            context.DrawText(label, new Point(plot.Left - label.Width - 8, y - label.Height / 2));
        }

        for (var i = 0; i <= 4; i++)
        {
            var ratio = i / 4.0;
            var x = plot.Left + ratio * plot.Width;
            var date = InterpolateDate(minDate, maxDate, ratio);
            context.DrawLine(gridPen, new Point(x, plot.Top), new Point(x, plot.Bottom));
            var label = CreateText(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), 11, Brushes.Gray);
            context.DrawText(label, new Point(x - label.Width / 2, plot.Bottom + 8));
        }

        context.DrawLine(axisPen, plot.BottomLeft, plot.BottomRight);
        context.DrawLine(axisPen, plot.BottomLeft, plot.TopLeft);

        var axisLabel = CreateText("Churn risk score", 12, Brushes.Gray);
        context.DrawText(axisLabel, new Point(plot.Left, 0));
    }

    private static void DrawSeries(
        DrawingContext context,
        IReadOnlyList<TimeSeriesGraphSeries> series,
        Rect plot,
        DateTime minDate,
        DateTime maxDate,
        double maxScore)
    {
        for (var index = 0; index < series.Count; index++)
        {
            var item = series[index];
            if (item.Points.Count == 0)
                continue;

            var brush = new SolidColorBrush(Palette[index % Palette.Length]);
            var pen = new Pen(brush, 1.6);
            var ordered = item.Points.OrderBy(point => point.Date).ToArray();

            for (var pointIndex = 1; pointIndex < ordered.Length; pointIndex++)
            {
                context.DrawLine(
                    pen,
                    Project(ordered[pointIndex - 1], plot, minDate, maxDate, maxScore),
                    Project(ordered[pointIndex], plot, minDate, maxDate, maxScore));
            }

            foreach (var point in ordered)
            {
                context.DrawEllipse(
                    brush,
                    null,
                    Project(point, plot, minDate, maxDate, maxScore),
                    3,
                    3);
            }
        }
    }

    private static void DrawLegend(
        DrawingContext context,
        IReadOnlyList<TimeSeriesGraphSeries> series,
        Rect plot)
    {
        var x = plot.Right + 24;
        var y = plot.Top;
        var title = CreateText("Top files", 13, Brushes.DimGray);
        context.DrawText(title, new Point(x, y));
        y += 26;

        for (var index = 0; index < Math.Min(series.Count, 22); index++)
        {
            var item = series[index];
            var brush = new SolidColorBrush(Palette[index % Palette.Length]);
            context.DrawRectangle(brush, null, new Rect(x, y + 5, 12, 3));

            var label = item.FilePath.Length > 40
                ? "..." + item.FilePath[^37..]
                : item.FilePath;
            var text = CreateText(label, 11, Brushes.DimGray);
            context.DrawText(text, new Point(x + 18, y));
            y += 20;
        }
    }

    private static Point Project(
        TimeSeriesGraphPoint point,
        Rect plot,
        DateTime minDate,
        DateTime maxDate,
        double maxScore)
    {
        var dateSpan = Math.Max(1, (maxDate - minDate).TotalSeconds);
        var xRatio = (point.Date - minDate).TotalSeconds / dateSpan;
        var yRatio = point.ChurnRiskScore / maxScore;

        return new Point(
            plot.Left + xRatio * plot.Width,
            plot.Bottom - yRatio * plot.Height);
    }

    private static DateTime InterpolateDate(DateTime minDate, DateTime maxDate, double ratio)
    {
        var ticks = minDate.Ticks + (long)((maxDate.Ticks - minDate.Ticks) * ratio);
        return new DateTime(ticks, DateTimeKind.Unspecified);
    }

    private static FormattedText CreateText(string text, double size, IBrush brush) =>
        new(
            text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            Typeface.Default,
            size,
            brush);
}
