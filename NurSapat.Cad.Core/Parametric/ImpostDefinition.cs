using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Core.Parametric;

public sealed class ImpostDefinition
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public SectionSplitOrientation Orientation { get; init; }

    public double ProfileWidth { get; init; }

    public CadLine Axis { get; init; }

    public double CutLength { get; init; }

    public double StartCutAngleDegrees { get; init; }

    public double EndCutAngleDegrees { get; init; }

    public ImpostDefinition(
        SectionSplitOrientation orientation,
        double profileWidth,
        CadLine axis,
        double startCutAngleDegrees = 90,
        double endCutAngleDegrees = 90)
    {
        Orientation = orientation;
        ProfileWidth = profileWidth;
        Axis = axis;
        CutLength = axis.Length;
        StartCutAngleDegrees = startCutAngleDegrees;
        EndCutAngleDegrees = endCutAngleDegrees;
    }
}
