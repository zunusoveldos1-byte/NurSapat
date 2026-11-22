namespace NurSapat.Cad.Tools;

/// <summary>Способ взаимодействия инструмента с пользователем.</summary>
public enum CadToolInteraction
{
    /// <summary>Инструмент остаётся активным и обрабатывает события мыши.</summary>
    Persistent,

    /// <summary>Одноразовое действие при выборе на панели (Undo, очистка и т.п.).</summary>
    InstantAction
}
