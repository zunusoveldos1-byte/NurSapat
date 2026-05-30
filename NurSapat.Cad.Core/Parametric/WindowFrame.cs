using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Parametric;

public sealed class WindowFrame
{
    public double Width { get; private set; }

    public double Height { get; private set; }

    public double ProfileWidth { get; set; }

    public ProfileKind ProfileKind { get; set; }

    public CadRect OuterBounds => new(0, Height, Width, Height);

    public CadRect InnerBounds => new(
        ProfileWidth,
        Height - ProfileWidth,
        Math.Max(0, Width - ProfileWidth * 2),
        Math.Max(0, Height - ProfileWidth * 2));

    public WindowFrame(double width, double height, double profileWidth, ProfileKind profileKind)
    {
        Resize(width, height);
        ProfileWidth = profileWidth;
        ProfileKind = profileKind;
    }

    public void Resize(double width, double height)
    {
        Width = Math.Max(300, width);
        Height = Math.Max(300, height);
    }
}
