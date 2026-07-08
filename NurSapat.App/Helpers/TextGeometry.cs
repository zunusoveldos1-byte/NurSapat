using System;
using Avalonia.Media;
using NurSapat.Cad.UI.Converters;
using SkiaSharp;

namespace NurSapat.App.Helpers;

/// <summary>
/// XAML-declarable resource that lazily builds an Avalonia <see cref="Geometry"/>
/// from a single font glyph.  Use in resource dictionaries alongside
/// <see cref="StreamGeometry"/> entries:
/// <code>
/// &lt;helpers:TextGeometry x:Key="Icon.UndoGlobal"
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

        var bounds = glyphPath.TightBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return null;

        using var normalized = new SKPath();

        // 1. Сбрасываем координаты: переносим левый верхний угол глифа строго в точку (0, 0)
        glyphPath.Transform(SKMatrix.CreateTranslation(-bounds.Left, -bounds.Top));

        // 2. Делаем иконку заметно меньше (0.70f — уменьшит её размер на 30% и визуально сделает линии тоньше)
        float scale = 0.70f;
        var scaleMatrix = SKMatrix.CreateScale(scale, scale, 0, 0);
        glyphPath.Transform(scaleMatrix);

        // 3. Получаем новые размеры иконки после масштабирования
        var scaledBounds = glyphPath.TightBounds;

        // 4. Вычисляем идеальные отступы, чтобы центрировать уменьшенную иконку внутри квадрата FontSize x FontSize
        float targetSize = (float)FontSize;
        float offsetX = (targetSize - scaledBounds.Width) / 2f - scaledBounds.Left;
        float offsetY = (targetSize - scaledBounds.Height) / 2f - scaledBounds.Top;

        // 5. Применяем финальный сдвиг в центр холста
        glyphPath.Transform(
            SKMatrix.CreateTranslation(offsetX, offsetY),
            normalized);

        var svgData = normalized.ToSvgPathData();
        if (string.IsNullOrEmpty(svgData))
            return null;

        return StreamGeometry.Parse(svgData);
    }
}
