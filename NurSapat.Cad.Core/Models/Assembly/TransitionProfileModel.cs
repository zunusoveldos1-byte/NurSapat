using NurSapat.Cad.Core.Geometry;
using NurSapat.Cad.Core.Models.Grid;

namespace NurSapat.Cad.Core.Models.Assembly;

/// <summary>
/// Переходной профиль как конструктивная граница между подрамами.
/// </summary>
public sealed class TransitionProfileModel
{
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Идентификатор узла-разделителя в дереве (если есть).</summary>
    public Guid? PartitionNodeId { get; init; }

    public GridLineOrientation Orientation { get; init; } = GridLineOrientation.Vertical;

    /// <summary>Абсолютная координата оси (X для Vertical, Y для Horizontal).</summary>
    public double AxisCoordinate { get; set; }

    public double ThicknessMm { get; init; }

    public Guid? MaterialId { get; init; }

    public Guid? HostGridLineId { get; init; }

    public Guid? LeftSubFrameId { get; set; }

    public Guid? RightSubFrameId { get; set; }

    public double InnerLeftFace => CadMath.RoundMm(AxisCoordinate - ThicknessMm / 2.0);

    public double InnerRightFace => CadMath.RoundMm(AxisCoordinate + ThicknessMm / 2.0);

    public double FaceTowardSubFrame(bool isLeftSubFrame) =>
        isLeftSubFrame ? InnerLeftFace : InnerRightFace;
}
