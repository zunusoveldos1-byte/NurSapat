using NurSapat.Cad.Core.Models;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

/// <summary>Ключи и правила инструментов создания (этап 3).</summary>
public static class CadCreationToolCatalog
{
    public const string SashFixed = "sash-fixed";
    public const string SashTurn = "sash-turn";
    public const string SashTilt = "sash-tilt";
    public const string SashSuspended = "sash-suspended";
    public const string SashTurnVertical = "sash-turn-v";
    public const string SashTurnHorizontal = "sash-turn-h";
    public const string SashSliding = "sash-sliding";
    public const string SashFolding = "sash-folding";
    public const string Lock = "lock";
    public const string OpeningLimiter = "opening-limiter";
    public const string DoorCloser = "door-closer";
    public const string Espagnolette = "espagnolette";
    public const string Bead = "bead";
    public const string GlazingWithBead = "glazing-with-bead";
    public const string AdditionalElements = "additional-elements";
    public const string Cornice = "cornice";
    public const string BlindVertical = "blind-v";
    public const string BlindHorizontal = "blind-h";
    public const string BlindControl = "blind-control";

    public static bool IsSashVariantTool(string? toolKey) =>
        toolKey is SashFixed or SashTurn or SashTilt or SashSuspended or SashTurnVertical
            or SashTurnHorizontal or SashSliding or SashFolding or "sash";

    public static bool RequiresSashDrag(string? toolKey) =>
        toolKey is not (SashFixed or null);

    public static GeometryElementKind ResolveSashKind(string? toolKey) =>
        toolKey == SashSliding ? GeometryElementKind.SlidingSash : GeometryElementKind.Sash;

    public static SashOpeningType ResolveOpeningType(string? toolKey, SashHingeSide hingeSide, double dragDeltaX)
    {
        return toolKey switch
        {
            SashFixed => SashOpeningType.Fixed,
            // Жест мыши задаёт сторону петель → чистое поворотное левое/правое (как в ghost-превью).
            SashTurn => ResolveSideTurnOpening(hingeSide),
            SashTilt => SashOpeningType.Tilt,
            SashSuspended => SashOpeningType.Suspended,
            SashTurnVertical => ResolveSideTurnOpening(hingeSide),
            SashTurnHorizontal => SashOpeningType.Turn,
            SashFolding => SashOpeningType.Folding,
            SashSliding => dragDeltaX < 0 ? SashOpeningType.SlideLeft : SashOpeningType.SlideRight,
            // Универсальный инструмент «Створка»: по умолчанию простое поворотное, не поворотно-откидное.
            "sash" or null => ResolveSideTurnOpening(hingeSide),
            _ => ResolveSideTurnOpening(hingeSide)
        };
    }

    /// <summary>
    /// Сторона петель по жесту: левое открывание → петли слева, правое → справа.
    /// </summary>
    public static SashOpeningType ResolveSideTurnOpening(SashHingeSide hingeSide) =>
        hingeSide switch
        {
            SashHingeSide.Left => SashOpeningType.LeftTurn,
            SashHingeSide.Right => SashOpeningType.RightTurn,
            _ => SashOpeningType.Turn
        };

    /// <summary>
    /// Сторона петель строго по типу открывания.
    /// LeftTurn → слева (основание треугольника слева); RightTurn → справа.
    /// Делегирует в <see cref="SashOpeningState"/> — единый источник истины.
    /// </summary>
    public static SashHingeSide ResolveHingeSideForOpening(SashOpeningType openingType, SashHingeSide fallback) =>
        SashOpeningState.ResolveHingeMountSide(openingType, fallback);

    /// <summary>
    /// Единый расчёт стороны петель для модели, призрака и отрисовки.
    /// </summary>
    public static SashHingeSide ResolveHingeMountSideForOpening(
        SashOpeningType openingType,
        SashHingeSide storedHingeSide) =>
        SashOpeningState.ResolveHingeMountSide(openingType, storedHingeSide);

    /// <summary>
    /// Атомарно выставляет OpeningType и согласованный HingeSide на створке.
    /// </summary>
    public static SashOpeningVisualState SyncSashOpeningAndHinge(
        GeometryElement sash,
        SashOpeningType openingType,
        SashHingeSide? preferredHinge = null) =>
        SashOpeningState.ApplyOpeningTypeChange(sash, openingType, preferredHinge);

    public static SashHingeSide NormalizeHingeForTool(string? toolKey, SashHingeSide fromDrag)
    {
        return toolKey switch
        {
            SashTilt => fromDrag is SashHingeSide.Top or SashHingeSide.Bottom
                ? fromDrag
                : SashHingeSide.Bottom,
            SashTurnVertical => fromDrag is SashHingeSide.Left or SashHingeSide.Right
                ? fromDrag
                : SashHingeSide.Left,
            SashTurnHorizontal => fromDrag is SashHingeSide.Top or SashHingeSide.Bottom
                ? fromDrag
                : SashHingeSide.Bottom,
            SashSuspended => SashHingeSide.Top,
            SashFixed => SashHingeSide.Left,
            _ => fromDrag
        };
    }

    public static bool ShouldEnableHardware(string? toolKey, SashOpeningType openingType) =>
        openingType is not SashOpeningType.Fixed
        && openingType is not (SashOpeningType.SlideLeft or SashOpeningType.SlideRight);

    /// <summary>Ручка ставится только инструментом «Ручка», не вместе с петлями.</summary>
    public static bool ShouldCreateHandle(SashOpeningType openingType) => false;

    public static bool IsPureSideTurn(SashOpeningType openingType) =>
        openingType is SashOpeningType.Turn or SashOpeningType.LeftTurn or SashOpeningType.RightTurn;

    public static string ResolveSashLabel(string? toolKey) => toolKey switch
    {
        SashFixed => "Створка глухая",
        SashTurn => "Створка поворотная",
        SashTilt => "Створка откидная",
        SashSuspended => "Створка подвесная",
        SashTurnVertical => "Створка поворотная (верт. ось)",
        SashTurnHorizontal => "Створка поворотная (гор. ось)",
        SashSliding => "Створка раздвижная",
        SashFolding => "Створка складная",
        _ => "Створка"
    };

    public static string? ResolveHardwareToolId(WindowToolMode mode) => mode switch
    {
        WindowToolMode.Lock => Lock,
        WindowToolMode.OpeningLimiter => OpeningLimiter,
        WindowToolMode.DoorCloser => DoorCloser,
        WindowToolMode.Espagnolette => Espagnolette,
        _ => null
    };

    public static string? ResolveBlindToolId(WindowToolMode mode) => mode switch
    {
        WindowToolMode.BlindVertical => BlindVertical,
        WindowToolMode.BlindHorizontal => BlindHorizontal,
        WindowToolMode.BlindControl => BlindControl,
        _ => null
    };

    public static string ResolveHardwareLabel(string toolId) => toolId switch
    {
        Lock => "Замок",
        OpeningLimiter => "Ограничитель открывания",
        DoorCloser => "Доводчик",
        Espagnolette => "Шпингалет",
        _ => "Фурнитура"
    };

    public static string ResolveAccessoryLabel(string toolId) => toolId switch
    {
        Cornice => "Карниз",
        BlindVertical => "Жалюзи вертикальные",
        BlindHorizontal => "Жалюзи горизонтальные",
        BlindControl => "Блок управления",
        AdditionalElements => "Монтажный элемент",
        _ => "Комплектующее"
    };

    public static bool IsClickPlacementMode(WindowToolMode mode) =>
        mode is WindowToolMode.BeadSystem
            or WindowToolMode.GlassWithBead
            or WindowToolMode.AdditionalElement
            or WindowToolMode.Cornice
            or WindowToolMode.BlindVertical
            or WindowToolMode.BlindHorizontal
            or WindowToolMode.BlindControl
            or WindowToolMode.Lock
            or WindowToolMode.OpeningLimiter
            or WindowToolMode.DoorCloser
            or WindowToolMode.Espagnolette
            or WindowToolMode.MosquitoNet
            or WindowToolMode.Glassing;
}
