namespace NurSapat.Cad.Tools;

/// <summary>
/// Идентификаторы инструментов CAD-редактора NurSapat.
/// Числовые значения соответствуют позициям в спецификации панели инструментов.
/// </summary>
public enum CadToolKind
{
  None = 0,

  // Группа 1 — системные и редактирование
  SelectElements = 1,
  DeleteLastPlaced = 2,
  Undo = 3,

  // Группа 2 — режимы выделения и фильтрации
  SelectObject = 4,
  SelectProfile = 5,
  SelectVertex = 6,

  // Группа 4 — габариты и заполнение (порядок в спецификации)
  AlignGlazingUnits = 7,
  AlignDeadLights = 8,

  // Группа 3 — геометрия и импосты
  SetVertexCoordinates = 9,
  SplitProfile = 10,
  OffsetImpostVertical = 11,
  OffsetImpostHorizontal = 12,
  AlignImposts = 13,

  ChangeOverallDimensions = 14,
  DeleteSelection = 15,
  ClearConstruction = 16,
  RadiusVertex = 17,
  Recalculate = 18,
  Properties = 19,
  Details = 20,
  ArcVertex = 21,
  RemoveArcVertex = 22,
  SplitArc = 23,
  SetRadius = 24,
  CopyConstruction = 25,
  PasteConstruction = 26,
  AddToStandard = 27,
  InsertFromStandard = 28
}
