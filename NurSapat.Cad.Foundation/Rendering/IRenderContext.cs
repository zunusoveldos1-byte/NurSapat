using NurSapat.Cad.Foundation.Primitives;

namespace NurSapat.Cad.Foundation.Rendering;

/// <summary>
/// Контекст отрисовки — абстракция поверхности для рендерера.
/// </summary>
/// <remarks>
/// Не содержит специфичного для UI API (никакого Avalonia/WPF).
/// Конкретные реализации (Avalonia, Skia, DirectX) создаются на Этапе 3.
/// </remarks>
public interface IRenderContext
{
    /// <summary>DPI контекста.</summary>
    double Dpi { get; }

    /// <summary>Область обрезки (в экранных координатах).</summary>
    Bounds2D ClipRect { get; }

    /// <summary>Размер целевой поверхности в пикселях.</summary>
    Size SurfaceSize { get; }
}
