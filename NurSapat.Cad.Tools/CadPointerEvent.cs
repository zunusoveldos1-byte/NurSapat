using NurSapat.Cad.Core.Geometry;

namespace NurSapat.Cad.Tools;

public readonly record struct CadPointerEvent(
    CadPoint WorldPoint,
    ScreenPoint ScreenPoint,
    bool IsCtrlDown,
    bool IsShiftDown,
    bool IsSpaceDown);
