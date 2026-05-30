namespace NurSapat.Cad.Core.Geometry;

public readonly record struct CadLine(CadPoint Start, CadPoint End)
{
    public CadPoint MidPoint => new((Start.X + End.X) / 2, (Start.Y + End.Y) / 2);

    public double Length => Start.DistanceTo(End);

    public CadVector Direction => (End - Start).Normalized();
}
