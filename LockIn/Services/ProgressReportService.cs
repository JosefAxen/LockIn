// LockIn/Services/ProgressReportService.cs
using LockIn.Models;
using SkiaSharp;

namespace LockIn.Services;

public record ProgressReportData(
    string ExerciseName,
    string MuscleGroupLabel,
    IReadOnlyList<ChartPoint> History,
    int TotalSessions
);

public class ProgressReportService
{
    private const int Width  = 1080;
    private const int Height = 1350;

    public async Task<string> CreateReportImageAsync(ProgressReportData data)
        => await Task.Run(() => RenderImage(data));

    private static string RenderImage(ProgressReportData data)
    {
        try
        {
            using var bitmap = new SKBitmap(Width, Height);
            using var canvas = new SKCanvas(bitmap);

            DrawBackground(canvas);
            DrawHeader(canvas);
            DrawExerciseInfo(canvas, data);
            DrawChart(canvas, data);
            DrawStats(canvas, data);
            DrawFooter(canvas, data);

            var path = Path.Combine(FileSystem.CacheDirectory, "progress_report.png");
            using var image   = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
            using var stream  = File.Create(path);
            encoded.SaveTo(stream);

            return path;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to render progress report image.", ex);
        }
    }

    // ─── Typeface helper (identisk med WorkoutShareService) ───────────────────

    private static SKTypeface LoadTypeface(string familyName)
    {
        var tf = SKTypeface.FromFamilyName(familyName);
        if (tf != null && tf.FamilyName != "Unknown")
            return tf;

        var fileName = familyName switch
        {
            "BebasNeue"    => "BebasNeue-Regular.ttf",
            "DMSansMedium" => "DMSans-Medium.ttf",
            _              => "DMSans-Regular.ttf"
        };

        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(fileName).GetAwaiter().GetResult();
            return SKTypeface.FromStream(stream) ?? SKTypeface.Default;
        }
        catch
        {
            return SKTypeface.Default;
        }
    }

    // ─── DrawBackground ───────────────────────────────────────────────────────

    private static void DrawBackground(SKCanvas canvas)
    {
        canvas.Clear(SKColor.Parse("#141418"));

        var center = new SKPoint(Width / 2f, Height / 2f);
        using var shader = SKShader.CreateRadialGradient(
            center, 700f,
            new[] { SKColor.Parse("#B8B8BC").WithAlpha((byte)(255 * 0.05f)), SKColors.Transparent },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { IsAntialias = true, Shader = shader };
        canvas.DrawRect(0, 0, Width, Height, paint);
    }

    // ─── DrawHeader (y: 0–140) ────────────────────────────────────────────────

    private static void DrawHeader(SKCanvas canvas)
    {
        using var tfBebas  = LoadTypeface("BebasNeue");
        using var tfMedium = LoadTypeface("DMSansMedium");

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface  = tfBebas,  TextSize  = 80f,
            Color       = SKColor.Parse("#B8B8BC"),   TextAlign = SKTextAlign.Left
        })
        {
            canvas.DrawText("VANA STRENGTH", 60f, 100f, paint);
        }

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface  = tfMedium, TextSize  = 24f,
            Color       = SKColor.Parse("#FF6B2C"),   TextAlign = SKTextAlign.Right
        })
        {
            canvas.DrawText("PROGRESS", Width - 60f, 100f, paint);
        }

        using var linePaint = new SKPaint
        {
            IsAntialias = true, Color = SKColor.Parse("#252530"),
            StrokeWidth = 2f,   Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(60f, 130f, Width - 60f, 130f, linePaint);
    }

    // ─── DrawExerciseInfo (y: 140–340) ────────────────────────────────────────

    private static void DrawExerciseInfo(SKCanvas canvas, ProgressReportData data)
    {
        using var tfBebas  = LoadTypeface("BebasNeue");
        using var tfMedium = LoadTypeface("DMSansMedium");

        var nameUpper = data.ExerciseName.ToUpperInvariant();

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface  = tfBebas,          TextSize  = 80f,
            Color       = SKColor.Parse("#E2E8F0"),            TextAlign = SKTextAlign.Left
        })
        {
            if (paint.MeasureText(nameUpper) > 960f)
                paint.TextSize = 52f;
            canvas.DrawText(nameUpper, 60f, 260f, paint);
        }

        var subLabel = data.MuscleGroupLabel.ToUpperInvariant()
                       + " · " + data.TotalSessions + " SESSIONER";
        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface  = tfMedium, TextSize  = 26f,
            Color       = SKColor.Parse("#A2A2A2"),   TextAlign = SKTextAlign.Left
        })
        {
            canvas.DrawText(subLabel, 60f, 310f, paint);
        }
    }

    // ─── DrawChart (y: 340–780) ───────────────────────────────────────────────

    private static void DrawChart(SKCanvas canvas, ProgressReportData data)
    {
        const float chartLeft   = 60f;
        const float chartRight  = 1020f;
        const float chartTop    = 360f;
        const float chartBottom = 760f;
        const float chartW      = chartRight - chartLeft;
        const float chartH      = chartBottom - chartTop;

        var history = data.History;
        if (history.Count == 0) return;

        double minVal = history.Min(p => p.Value);
        double maxVal = history.Max(p => p.Value);
        double range  = maxVal - minVal < 1 ? 1 : maxVal - minVal;

        float XAt(int i)      => chartLeft + (history.Count > 1 ? (float)i / (history.Count - 1) * chartW : chartW / 2f);
        float YAt(double v)   => chartBottom - (float)((v - minVal) / range) * chartH;

        // Fill area
        using (var fillPath = new SKPath())
        {
            fillPath.MoveTo(XAt(0), chartBottom);
            for (int i = 0; i < history.Count; i++)
                fillPath.LineTo(XAt(i), YAt(history[i].Value));
            fillPath.LineTo(XAt(history.Count - 1), chartBottom);
            fillPath.Close();

            using var fillPaint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColor.Parse("#FF6B2C").WithAlpha(40),
                Style = SKPaintStyle.Fill
            };
            canvas.DrawPath(fillPath, fillPaint);
        }

        // Line
        using (var linePath = new SKPath())
        {
            linePath.MoveTo(XAt(0), YAt(history[0].Value));
            for (int i = 1; i < history.Count; i++)
                linePath.LineTo(XAt(i), YAt(history[i].Value));

            using var linePaint = new SKPaint
            {
                IsAntialias = true, Color = SKColor.Parse("#FF6B2C"),
                StrokeWidth = 3f, Style = SKPaintStyle.Stroke,
                StrokeCap = SKStrokeCap.Round, StrokeJoin = SKStrokeJoin.Round
            };
            canvas.DrawPath(linePath, linePaint);
        }

        // Last point dot
        float lastX = XAt(history.Count - 1);
        float lastY = YAt(history[history.Count - 1].Value);
        using (var dotPaint = new SKPaint { IsAntialias = true, Color = SKColor.Parse("#FF6B2C"), Style = SKPaintStyle.Fill })
            canvas.DrawCircle(lastX, lastY, 10f, dotPaint);

        // X-axis labels (max 5)
        using var tfRegular = LoadTypeface("DMSansRegular");
        using var lblPaint = new SKPaint
        {
            IsAntialias = true, Typeface = tfRegular, TextSize = 22f,
            Color = SKColor.Parse("#A2A2A2"), TextAlign = SKTextAlign.Center
        };
        int step = Math.Max(1, (history.Count - 1) / 4);
        for (int i = 0; i < history.Count; i += step)
            canvas.DrawText(history[i].Date.ToString("MMM d"), XAt(i), chartBottom + 40f, lblPaint);
    }

    // ─── DrawStats (y: 820–1050) ──────────────────────────────────────────────

    private static void DrawStats(SKCanvas canvas, ProgressReportData data)
    {
        const float cardY  = 820f;
        const float cardH  = 200f;
        const float margin = 60f;
        const float gap    = 20f;
        float cardW = (Width - margin * 2f - gap) / 2f;

        double bestRm   = data.History.Count > 0 ? data.History.Max(p => p.Value) : 0;
        string bestRmStr = bestRm > 0 ? $"{bestRm:F0}" : "–";

        string improvStr = "–";
        if (data.History.Count >= 2)
        {
            double first = data.History[0].Value;
            double last  = data.History[data.History.Count - 1].Value;
            double pct   = (last - first) / first * 100;
            improvStr = pct >= 0 ? $"+{pct:F0}%" : $"{pct:F0}%";
        }

        (string Value, string Label, string Color)[] cards =
        [
            (bestRmStr, "BÄSTA 1RM",    "#FF6B2C"),
            (improvStr, "FÖRBÄTTRING",  "#38BDF8")
        ];

        using var tfBebas  = LoadTypeface("BebasNeue");
        using var tfMedium = LoadTypeface("DMSansMedium");

        for (int i = 0; i < 2; i++)
        {
            float x = margin + i * (cardW + gap);

            using (var bgPaint = new SKPaint
            {
                IsAntialias = true, Color = SKColor.Parse("#222228"), Style = SKPaintStyle.Fill
            })
            {
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(x, cardY, x + cardW, cardY + cardH), 20f, 20f), bgPaint);
            }

            float cx = x + cardW / 2f;

            using var numPaint = new SKPaint
            {
                IsAntialias = true, Typeface = tfBebas, TextSize = 88f,
                Color = SKColor.Parse(cards[i].Color), TextAlign = SKTextAlign.Center
            };
            numPaint.GetFontMetrics(out var fm);
            float numY   = cardY + (cardH - (fm.Descent - fm.Ascent)) / 2f - fm.Ascent - 15f;
            canvas.DrawText(cards[i].Value, cx, numY, numPaint);

            using var lblPaint2 = new SKPaint
            {
                IsAntialias = true, Typeface = tfMedium, TextSize = 22f,
                Color = SKColor.Parse("#A2A2A2"), TextAlign = SKTextAlign.Center
            };
            canvas.DrawText(cards[i].Label, cx, numY + fm.Descent + 14f, lblPaint2);
        }
    }

    // ─── DrawFooter (y: 1050–1350) ────────────────────────────────────────────

    private static void DrawFooter(SKCanvas canvas, ProgressReportData data)
    {
        using var linePaint = new SKPaint
        {
            IsAntialias = true, Color = SKColor.Parse("#252530"),
            StrokeWidth = 2f, Style = SKPaintStyle.Stroke
        };
        canvas.DrawLine(60f, 1070f, Width - 60f, 1070f, linePaint);

        using var tfRegular = LoadTypeface("DMSansRegular");
        using var tfMedium  = LoadTypeface("DMSansMedium");

        string dateRange = data.History.Count >= 2
            ? $"{data.History[0].Date:MMM d, yyyy} → {data.History[data.History.Count - 1].Date:MMM d, yyyy}"
            : data.History.Count == 1
                ? data.History[0].Date.ToString("MMM d, yyyy")
                : "";

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface = tfRegular, TextSize = 22f,
            Color = SKColor.Parse("#A2A2A2"), TextAlign = SKTextAlign.Left
        })
        {
            canvas.DrawText(dateRange, 60f, 1130f, paint);
        }

        using (var paint = new SKPaint
        {
            IsAntialias = true, Typeface = tfMedium, TextSize = 22f,
            Color = SKColor.Parse("#A2A2A2"), TextAlign = SKTextAlign.Left
        })
        {
            canvas.DrawText("VANA STRENGTH", 60f, 1200f, paint);
        }
    }
}
