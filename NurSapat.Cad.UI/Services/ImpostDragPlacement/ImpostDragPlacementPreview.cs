using NurSapat.Cad.Core.Models;



namespace NurSapat.Cad.UI.Services.ImpostDragPlacement;



/// <summary>

/// Состояние предпросмотра размещения импоста (только визуальный слой, не в модели).

/// </summary>

public sealed class ImpostDragPlacementPreview

{

    public required FrameNode Leaf { get; init; }



    public required SplitOrientation Orientation { get; init; }



    public required double AxisValue { get; init; }



    public required double ImpostWidthMm { get; init; }



    public required ImpostLineStyle LineStyle { get; init; }



    public required bool IsValid { get; init; }

}

