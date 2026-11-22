namespace NurSapat.Cad.Foundation.Rendering;

/// <summary>
/// Объект, способный предоставить данные для отрисовки.
/// </summary>
public interface IRenderable
{
    /// <summary>Получить данные отрисовки (или null, если объект не отрисовывается).</summary>
    RenderData? GetRenderData();
}
