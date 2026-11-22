using Avalonia.Media;

namespace NurSapat.Cad.UI.Converters;

/// <summary>
/// Implemented by XAML-declared objects (e.g. <c>TextGeometry</c>) that can
/// lazily produce an Avalonia <see cref="Geometry"/> at runtime.
/// The <see cref="ResourceKeyToGeometryConverter"/> checks for this interface
/// when the resource value is not already a <see cref="Geometry"/>.
/// </summary>
public interface IGeometrySource
{
    Geometry? ToGeometry();
}
