using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models.Grid;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Единая точка inner-face / barrier для переходных профилей.
/// </summary>
public interface ITransitionGeometryResolver
{
    TransitionProfileModel? FindAdjacentTransition(SubFrame subFrame, bool leftSide);

    double ResolveInnerFace(TransitionProfileModel profile, bool towardLeftSubFrame);

    (double Left, double Right) ClipHorizontalSpan(
        SubFrame subFrame,
        double spanLeftWorld,
        double spanRightWorld,
        double axisY);
}

/// <summary>
/// Реализация на основе ConstructionAssembly.
/// </summary>
public sealed class TransitionGeometryResolver : ITransitionGeometryResolver
{
    private readonly ConstructionAssembly _assembly;

    public TransitionGeometryResolver(ConstructionAssembly assembly)
    {
        _assembly = assembly;
    }

    public TransitionProfileModel? FindAdjacentTransition(SubFrame subFrame, bool leftSide) =>
        _assembly.FindAdjacentTransition(subFrame, leftSide);

    public double ResolveInnerFace(TransitionProfileModel profile, bool towardLeftSubFrame) =>
        profile.FaceTowardSubFrame(towardLeftSubFrame);

    public (double Left, double Right) ClipHorizontalSpan(
        SubFrame subFrame,
        double spanLeftWorld,
        double spanRightWorld,
        double axisY) =>
        SubFrameCoordinateSystem.ClipHorizontalSpan(
            subFrame,
            _assembly,
            spanLeftWorld,
            spanRightWorld,
            axisY);
}
