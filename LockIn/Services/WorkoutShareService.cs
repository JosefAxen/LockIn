using SkiaSharp;

namespace LockIn.Services;

public record ShareImageData(
    string TemplateName,
    string DateDisplay,
    string Duration,
    string TotalVolume,
    string TotalSets,
    string PrCount,
    IReadOnlyList<(string Name, double Fraction)> TopMuscles
);

public class WorkoutShareService
{
    private const int Size = 1080;

    public async Task<string> CreateShareImageAsync(ShareImageData data)
    {
        return await Task.Run(() => RenderImage(data));
    }

    private static string RenderImage(ShareImageData data)
    {
        try
        {
            using var bitmap = new SKBitmap(Size, Size);
            using var canvas = new SKCanvas(bitmap);

            DrawBackground(canvas);
            DrawHeader(canvas, data);
            DrawHeroSection(canvas, data);
            DrawStatsSection(canvas, data);
            DrawMuscleSection(canvas, data);
            DrawFooter(canvas);

            var path = Path.Combine(FileSystem.CacheDirectory, "share_preview.png");
            using var image = SKImage.FromBitmap(bitmap);
            using var encoded = image.Encode(SKEncodedImageFormat.Png, 90);
            using var stream = File.Create(path);
            encoded.SaveTo(stream);

            return path;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to render share image.", ex);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Typeface helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static SKTypeface LoadTypeface(string familyName)
    {
        var tf = SKTypeface.FromFamilyName(familyName);
        if (tf != null && tf.FamilyName != "Unknown")
            return tf;

        // Fallback: load from embedded font file
        var fileName = familyName switch
        {
            "BebasNeue"    => "BebasNeue-Regular.ttf",
            "DMSansMedium" => "DMSans-Medium.ttf",
            "DMSansRegular" or _ => "DMSans-Regular.ttf"
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

    // ─────────────────────────────────────────────────────────────────────────
    // DrawBackground
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawBackground(SKCanvas canvas)
    {
        // Solid base
        canvas.Clear(SKColor.Parse("#141418"));

        // Subtle radial gradient overlay
        var center = new SKPoint(Size / 2f, Size / 2f);
        using var shader = SKShader.CreateRadialGradient(
            center,
            600f,
            new[] { SKColor.Parse("#B8B8BC").WithAlpha((byte)(255 * 0.06f)), SKColors.Transparent },
            new[] { 0f, 1f },
            SKShaderTileMode.Clamp);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Shader = shader
        };
        canvas.DrawRect(0, 0, Size, Size, paint);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DrawHeader  (y: 0–200 px)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawHeader(SKCanvas canvas, ShareImageData data)
    {
        // "LOCKIN"
        using (var tf = LoadTypeface("BebasNeue"))
        using (var paint = new SKPaint
        {
            IsAntialias = true,
            Typeface    = tf,
            TextSize    = 96f,
            Color       = SKColor.Parse("#B8B8BC"),
            TextAlign   = SKTextAlign.Center
        })
        {
            canvas.DrawText("LOCKIN", Size / 2f, 130f, paint);
        }

        // "Strength Training"
        using (var tf = LoadTypeface("DMSansMedium"))
        using (var paint = new SKPaint
        {
            IsAntialias = true,
            Typeface    = tf,
            TextSize    = 22f,
            Color       = SKColor.Parse("#A2A2A2"),
            TextAlign   = SKTextAlign.Center
        })
        {
            canvas.DrawText("Strength Training", Size / 2f, 168f, paint);
        }

        // Divider line
        using var linePaint = new SKPaint
        {
            IsAntialias = true,
            Color       = SKColor.Parse("#252530"),
            StrokeWidth = 2f,
            Style       = SKPaintStyle.Stroke
        };
        canvas.DrawLine(80f, 196f, Size - 80f, 196f, linePaint);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DrawHeroSection  (y: 200–480 px)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawHeroSection(SKCanvas canvas, ShareImageData data)
    {
        var templateUpper = data.TemplateName.ToUpperInvariant();

        // Template name
        using (var tf = LoadTypeface("BebasNeue"))
        using (var paint = new SKPaint
        {
            IsAntialias = true,
            Typeface    = tf,
            TextSize    = 72f,
            Color       = SKColor.Parse("#E2E8F0"),
            TextAlign   = SKTextAlign.Center
        })
        {
            // Shrink if too wide
            if (paint.MeasureText(templateUpper) > 920f)
                paint.TextSize = 52f;

            canvas.DrawText(templateUpper, Size / 2f, 310f, paint);
        }

        // Date + Duration
        var subtitle = data.DateDisplay + "  ·  " + data.Duration;
        using (var tf = LoadTypeface("DMSansRegular"))
        using (var paint = new SKPaint
        {
            IsAntialias = true,
            Typeface    = tf,
            TextSize    = 26f,
            Color       = SKColor.Parse("#A2A2A2"),
            TextAlign   = SKTextAlign.Center
        })
        {
            canvas.DrawText(subtitle, Size / 2f, 368f, paint);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DrawStatsSection  (y: 480–760 px)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawStatsSection(SKCanvas canvas, ShareImageData data)
    {
        const float margin    = 40f;
        const float gap       = 20f;
        const float cardY     = 500f;
        const float cardH     = 220f;
        float cardW = (Size - margin * 2f - gap * 2f) / 3f;

        var cards = new[]
        {
            (Value: data.TotalVolume, Label: "KG VOLYM", Color: "#38BDF8"),
            (Value: data.TotalSets,   Label: "SETS",     Color: "#A78BFA"),
            (Value: data.PrCount,     Label: "PR",       Color: data.PrCount == "0" ? "#A2A2A2" : "#FBBF24")
        };

        using var tfBebas   = LoadTypeface("BebasNeue");
        using var tfMedium  = LoadTypeface("DMSansMedium");

        for (int i = 0; i < cards.Length; i++)
        {
            float x = margin + i * (cardW + gap);

            // Card background
            using (var bgPaint = new SKPaint
            {
                IsAntialias = true,
                Color       = SKColor.Parse("#222228"),
                Style       = SKPaintStyle.Fill
            })
            {
                var rrect = new SKRoundRect(new SKRect(x, cardY, x + cardW, cardY + cardH), 20f, 20f);
                canvas.DrawRoundRect(rrect, bgPaint);
            }

            float cx = x + cardW / 2f;

            // Number — vertically centered in card
            using (var numPaint = new SKPaint
            {
                IsAntialias = true,
                Typeface    = tfBebas,
                TextSize    = 88f,
                Color       = SKColor.Parse(cards[i].Color),
                TextAlign   = SKTextAlign.Center
            })
            {
                numPaint.GetFontMetrics(out var fm);
                float textH  = fm.Descent - fm.Ascent;
                float numY   = cardY + (cardH - textH) / 2f - fm.Ascent;
                canvas.DrawText(cards[i].Value, cx, numY, numPaint);

                // Label 10 px below baseline
                float labelY = numY + 10f;
                using var lblPaint = new SKPaint
                {
                    IsAntialias = true,
                    Typeface    = tfMedium,
                    TextSize    = 20f,
                    Color       = SKColor.Parse("#A2A2A2"),
                    TextAlign   = SKTextAlign.Center
                };
                canvas.DrawText(cards[i].Label, cx, labelY, lblPaint);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DrawMuscleSection  (y: 760–960 px)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawMuscleSection(SKCanvas canvas, ShareImageData data)
    {
        const float firstRowY  = 800f;
        const float rowSpacing = 50f;
        const float barStartX  = 260f;
        const float barWidth   = 480f;
        const float barH       = 6f;
        const float nameLimitW = 200f;

        var muscles = data.TopMuscles.Take(3).ToList();

        using var tfMedium = LoadTypeface("DMSansMedium");
        using var trackPaint = new SKPaint
        {
            IsAntialias = true,
            Color       = SKColor.Parse("#2E2E36"),
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = barH,
            StrokeCap   = SKStrokeCap.Round
        };
        using var activePaint = new SKPaint
        {
            IsAntialias = true,
            Color       = SKColor.Parse("#B8B8BC"),
            Style       = SKPaintStyle.Stroke,
            StrokeWidth = barH,
            StrokeCap   = SKStrokeCap.Round
        };

        for (int i = 0; i < muscles.Count; i++)
        {
            var (name, fraction) = muscles[i];
            float rowY = firstRowY + i * rowSpacing;

            // Muscle name (left-aligned, max 200 px)
            using (var namePaint = new SKPaint
            {
                IsAntialias = true,
                Typeface    = tfMedium,
                TextSize    = 22f,
                Color       = SKColor.Parse("#A2A2A2"),
                TextAlign   = SKTextAlign.Left
            })
            {
                // Truncate if needed
                string displayName = name;
                while (displayName.Length > 0 && namePaint.MeasureText(displayName) > nameLimitW)
                    displayName = displayName[..^1];

                namePaint.GetFontMetrics(out var fm);
                float textY = rowY - fm.Ascent / 2f; // vertically center on row
                canvas.DrawText(displayName, 80f, textY, namePaint);
            }

            // Bar track
            float barY = rowY;
            canvas.DrawLine(barStartX, barY, barStartX + barWidth, barY, trackPaint);

            // Active fill
            if (fraction > 0)
            {
                float activeEnd = barStartX + (float)(barWidth * Math.Clamp(fraction, 0.0, 1.0));
                canvas.DrawLine(barStartX, barY, activeEnd, barY, activePaint);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DrawFooter  (y: 960–1080 px)
    // ─────────────────────────────────────────────────────────────────────────

    private static void DrawFooter(SKCanvas canvas)
    {
        // Divider line
        using (var linePaint = new SKPaint
        {
            IsAntialias = true,
            Color       = SKColor.Parse("#252530"),
            StrokeWidth = 2f,
            Style       = SKPaintStyle.Stroke
        })
        {
            canvas.DrawLine(80f, 964f, Size - 80f, 964f, linePaint);
        }

        using var tfMedium  = LoadTypeface("DMSansMedium");
        using var tfRegular = LoadTypeface("DMSansRegular");

        // "VANA STRENGTH" — left-aligned
        using (var paint = new SKPaint
        {
            IsAntialias = true,
            Typeface    = tfMedium,
            TextSize    = 20f,
            Color       = SKColor.Parse("#A2A2A2"),
            TextAlign   = SKTextAlign.Left
        })
        {
            canvas.DrawText("VANA STRENGTH", 80f, 1010f, paint);
        }

        // "lockin.app" — right-aligned
        using (var paint = new SKPaint
        {
            IsAntialias = true,
            Typeface    = tfRegular,
            TextSize    = 20f,
            Color       = SKColor.Parse("#A2A2A2"),
            TextAlign   = SKTextAlign.Right
        })
        {
            canvas.DrawText("lockin.app", 1000f, 1010f, paint);
        }
    }
}
