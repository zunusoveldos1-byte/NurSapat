using Avalonia.Media.Imaging;
using PDFtoImage;
using SkiaSharp;
using System.Diagnostics;

namespace NurSapat.App.Services.Help;

public sealed class UserGuideService : IUserGuideService
{
    private static readonly string[] CandidatePaths =
    [
        Path.Combine(AppContext.BaseDirectory, "Assets", "Help", "UserGuide.pdf"),
        Path.Combine(AppContext.BaseDirectory, "Help", "UserGuide.pdf")
    ];

    public string GuideFilePath { get; } = ResolveGuidePath();

    public bool GuideExists
    {
        get
        {
            EnsureSampleGuide();
            return File.Exists(GuideFilePath);
        }
    }

    public IReadOnlyList<Bitmap> LoadPreviewPages(int maxPages = 50)
    {
        EnsureSampleGuide();

        if (!File.Exists(GuideFilePath))
        {
            return [];
        }

        try
        {
            var bytes = File.ReadAllBytes(GuideFilePath);
            var renderOptions = new RenderOptions(Dpi: 120);
            var result = new List<Bitmap>();

            foreach (var page in Conversion.ToImages(bytes, options: renderOptions).Take(maxPages))
            {
                result.Add(ToAvaloniaBitmap(page));
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    public void OpenInDefaultViewer()
    {
        EnsureSampleGuide();

        if (!File.Exists(GuideFilePath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo(GuideFilePath)
        {
            UseShellExecute = true
        });
    }

    private void EnsureSampleGuide()
    {
        if (File.Exists(GuideFilePath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(GuideFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        CreateSampleGuidePdf(GuideFilePath);
    }

    private static void CreateSampleGuidePdf(string path)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var document = SKDocument.CreatePdf(stream);
        using var canvas = document.BeginPage(595, 842);

        using var titleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 28);
        using var titlePaint = new SKPaint
        {
            Color = SKColor.Parse("#0F172A"),
            IsAntialias = true
        };

        using var subtitleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), 18);
        using var subtitlePaint = new SKPaint
        {
            Color = SKColor.Parse("#2563EB"),
            IsAntialias = true
        };

        using var bodyFont = new SKFont(SKTypeface.FromFamilyName("Arial"), 13);
        using var bodyPaint = new SKPaint
        {
            Color = SKColor.Parse("#475569"),
            IsAntialias = true
        };

        canvas.DrawText("NurSapat", 50, 72, SKTextAlign.Left, titleFont, titlePaint);
        canvas.DrawText("Rukovodstvo polzovatelya", 50, 108, SKTextAlign.Left, subtitleFont, subtitlePaint);

        var lines =
            new[]
            {
                "CAD / ERP sistema dlya okonnogo proizvodstva",
                string.Empty,
                "1. Bystryj start",
                "   Sozdayte novyj zakaz cherez menyu Zakaz.",
                "   Otkroyte vkladku Chertezh dlya proektirovaniya.",
                string.Empty,
                "2. Konstruktor",
                "   Dobavte ramu, imposty, stvorki i steklopakety.",
                string.Empty,
                "3. Zakaz i raschet",
                "   Proverte specifikaciyu i stoimost na liste zakaza.",
                string.Empty,
                "4. Pechat i eksport",
                "   Ispolzuyte menyu Servis dlya vygruzki dannyh.",
                string.Empty,
                "Versiya 9.0.0"
            };

        var y = 150f;
        foreach (var line in lines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                canvas.DrawText(line, 50, y, SKTextAlign.Left, bodyFont, bodyPaint);
            }

            y += 22f;
        }

        document.EndPage();
        document.Close();
    }

    private static string ResolveGuidePath()
    {
        foreach (var path in CandidatePaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return CandidatePaths[0];
    }

    private static Bitmap ToAvaloniaBitmap(SKBitmap skBitmap)
    {
        using var image = SKImage.FromBitmap(skBitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 92);
        using var stream = new MemoryStream(data.ToArray());
        return new Bitmap(stream);
    }
}
