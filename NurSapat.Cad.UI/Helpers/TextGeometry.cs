using Avalonia.Media;
using NurSapat.Cad.UI.Converters;
using SkiaSharp;

namespace NurSapat.Cad.UI.Helpers;

/// <summary>
/// XAML-declarable resource that lazily builds an Avalonia <see cref="Geometry"/>
/// from a single font glyph.  Use in resource dictionaries alongside
/// <see cref="StreamGeometry"/> entries:
/// <code>
/// &lt;helpers:TextGeometry x:Key="CadIcon.Undo"
///                        Text="&amp;#xE7A7;"
///                        FontFamilyName="Segoe MDL2 Assets"
///                        FontSize="20"/&gt;
/// </code>
/// The <see cref="ResourceKeyToGeometryConverter"/> recognises the
/// <see cref="IGeometrySource"/> interface and calls <see cref="ToGeometry"/>.
/// </summary>
public sealed class TextGeometry : IGeometrySource
{
    public string? Text { get; set; }

    public string FontFamilyName { get; set; } = "Segoe MDL2 Assets";

    public double FontSize { get; set; } = 20;

    private Geometry? _cached;

    public Geometry? ToGeometry()
    {
        if (_cached is not null)
            return _cached;

        if (string.IsNullOrEmpty(Text))
            return null;

        try
        {
            _cached = BuildGeometry();
        }
        catch
        {
            _cached = null;
        }

        return _cached;
    }

    private Geometry? BuildGeometry()
    {
        if (string.IsNullOrEmpty(Text))
            return null;

        using var typeface = SKTypeface.FromFamilyName(FontFamilyName);
        if (typeface is null)
            return null;

        using var font = new SKFont(typeface, (float)FontSize);

        var glyphIds = font.GetGlyphs(Text);
        if (glyphIds.Length == 0)
            return null;

        using var glyphPath = font.GetGlyphPath(glyphIds[0]);
        if (glyphPath is null || glyphPath.IsEmpty)
            return null;

        // Получаем плотные границы глифа
        var bounds = glyphPath.TightBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        float targetSize = (float)FontSize;

        // 1. Вычисляем масштаб, чтобы глиф занимал примерно 80% от целевого размера
        float margin = 0.8f; // 80% — остаётся место для отступов
        float scaleX = (targetSize * margin) / bounds.Width;
        float scaleY = (targetSize * margin) / bounds.Height;
        float scale = Math.Min(scaleX, scaleY); // единый масштаб для сохранения пропорций

        // 2. Масштабируем путь относительно центра его bounding box
        float centerX = bounds.Left + bounds.Width / 2;
        float centerY = bounds.Top + bounds.Height / 2;
        var scaleMatrix = SKMatrix.CreateScale(scale, scale, centerX, centerY);
        using var scaledPath = new SKPath();
        glyphPath.Transform(scaleMatrix, scaledPath);

        // 3. Получаем границы масштабированного пути
        var scaledBounds = scaledPath.TightBounds;

        // 4. Вычисляем смещение для центрирования внутри квадрата targetSize
        float offsetX = (targetSize - scaledBounds.Width) / 2 - scaledBounds.Left;
        float offsetY = (targetSize - scaledBounds.Height) / 2 - scaledBounds.Top;

        // 5. Применяем финальное смещение
        using var finalPath = new SKPath();
        scaledPath.Transform(SKMatrix.CreateTranslation(offsetX, offsetY), finalPath);

        // 6. Конвертируем в SVG-строку и создаём StreamGeometry
        var svgData = finalPath.ToSvgPathData();
        if (string.IsNullOrEmpty(svgData))
            return null;

        return StreamGeometry.Parse(svgData);
    }
}
