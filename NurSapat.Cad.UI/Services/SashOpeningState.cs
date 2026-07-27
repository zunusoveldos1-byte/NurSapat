using System.Diagnostics;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

/// <summary>
/// Единый атомарный снимок геометрии открывания створки.
/// Источник истины — <see cref="OpeningType"/> (+ явная сторона для TiltTurn/Turn/Tilt).
/// HingeSide / треугольник / ghost-петли всегда совпадают.
/// </summary>
public readonly record struct SashOpeningVisualState(
    SashOpeningType OpeningType,
    SashHingeSide HingeSide,
    SashHingeSide TriangleSide,
    SashHingeSide GhostHingeSide)
{
    public SashHingeSide HingeMountSide => HingeSide;
}

/// <summary>
/// Единая модель обновления открывания створки: один расчёт → атомарная запись → один кадр UI.
/// </summary>
public static class SashOpeningState
{
    /// <summary>
    /// Вычисляет согласованное состояние без мутации модели.
    /// </summary>
    public static SashOpeningVisualState Resolve(
        SashOpeningType openingType,
        SashHingeSide? preferredHinge = null)
    {
        var hinge = ResolveHingeMountSide(openingType, preferredHinge ?? SashHingeSide.Left);
        // Треугольник, реальные петли и ghost — одна сторона.
        return new SashOpeningVisualState(openingType, hinge, hinge, hinge);
    }

    public static SashOpeningVisualState Resolve(GeometryElement sash) =>
        Resolve(
            sash.OpeningType ?? SashOpeningType.Fixed,
            sash.HingeSide);

    /// <summary>
    /// Атомарно применяет новый тип открывания: OpeningType + HingeSide пишутся вместе,
    /// без промежуточного состояния для рендера.
    /// </summary>
    public static SashOpeningVisualState ApplyOpeningTypeChange(
        GeometryElement sash,
        SashOpeningType newOpeningType,
        SashHingeSide? preferredHinge = null)
    {
        var oldOpening = sash.OpeningType;
        var oldHinge = sash.HingeSide;
        var state = Resolve(newOpeningType, preferredHinge ?? sash.HingeSide);

        // Критично: обе записи до любого InvalidateVisual / PropertyChanged.
        sash.OpeningType = state.OpeningType;
        sash.HingeSide = state.HingeSide;

        LogOpeningTypeChange(oldOpening, oldHinge, state);
        return state;
    }

    /// <summary>
    /// Сторона петель строго из OpeningType (для Left/RightTurn — без кэша/fallback).
    /// </summary>
    public static SashHingeSide ResolveHingeMountSide(
        SashOpeningType openingType,
        SashHingeSide preferredOrStored) =>
        openingType switch
        {
            SashOpeningType.LeftTurn => SashHingeSide.Left,
            SashOpeningType.RightTurn => SashHingeSide.Right,
            SashOpeningType.TiltTurn or SashOpeningType.Turn or SashOpeningType.Tilt
                when preferredOrStored is SashHingeSide.Left or SashHingeSide.Right
                => preferredOrStored,
            SashOpeningType.TiltTurn or SashOpeningType.Turn or SashOpeningType.Tilt
                => SashHingeSide.Left,
            SashOpeningType.Suspended when preferredOrStored is SashHingeSide.Top
                => SashHingeSide.Top,
            SashOpeningType.Suspended
                => preferredOrStored is SashHingeSide.Left or SashHingeSide.Right
                    ? preferredOrStored
                    : SashHingeSide.Top,
            _ => preferredOrStored is SashHingeSide.Left or SashHingeSide.Right
                ? preferredOrStored
                : SashHingeSide.Left
        };

    private static void LogOpeningTypeChange(
        SashOpeningType? oldOpening,
        SashHingeSide? oldHinge,
        SashOpeningVisualState state)
    {
        Debug.WriteLine(
            $"""
            OpeningType changed:
            Old: {oldOpening?.ToString() ?? "null"} (HingeSide={oldHinge?.ToString() ?? "null"})
            New: {state.OpeningType}

            Calculated HingeSide: {state.HingeSide}
            Triangle side: {state.TriangleSide}
            Ghost hinge side: {state.GhostHingeSide}
            """);
    }
}
