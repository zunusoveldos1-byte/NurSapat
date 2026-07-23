using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Tools;
using NurSapat.Cad.UI.Models;

namespace NurSapat.Cad.UI.Services;

public static class CadToolbarRegistry
{
    public static IReadOnlyList<CadToolbarItem> All { get; } = BuildAll();

    public static IReadOnlyList<IReadOnlyList<CadToolbarItem>> Blocks { get; } =
        Enumerable.Range(0, 4)
            .Select(i => (IReadOnlyList<CadToolbarItem>)All.Where(t => t.BlockIndex == i).ToList())
            .ToList();

    private static IReadOnlyList<CadToolbarItem> BuildAll()
    {
        var items = new List<CadToolbarItem>();

        void Add(
            int block,
            string sb,
            string hint,
            WindowToolMode mode,
            string glyph,
            string? cadToolId = null,
            CadToolKind? toolKind = null,
            bool instant = false) =>
            items.Add(new CadToolbarItem
            {
                BlockIndex = block,
                SpeedButton = sb,
                Hint = hint,
                Mode = mode,
                IconGlyph = glyph,
                CadToolId = cadToolId,
                ToolKind = toolKind,
                IsInstantAction = instant
            });

        // Блок 1 — выделение и навигация (16 базовых инструментов CAD)
        Add(0, "SpeedButton44", "Выделение элементов", WindowToolMode.SelectElement, "\uE8A5", "select-elements", CadToolKind.SelectElements);
        Add(0, "SpeedButton46", "Удалить последний элемент", WindowToolMode.Other, "\uE74D", "delete-last-placed", CadToolKind.DeleteLastPlaced, instant: true);
        Add(0, "SpeedButton1", "Отменить действие", WindowToolMode.Other, "\uE7A7", "undo", CadToolKind.Undo, instant: true);
        Add(0, "SpeedButton62", "Выделение объекта", WindowToolMode.Select, "\uE8C1", "select-object", CadToolKind.SelectObject);
        Add(0, "SpeedButton63", "Выделение профиля", WindowToolMode.SelectProfile, "\uE8B0", "select-profile", CadToolKind.SelectProfile);
        Add(0, "SpeedButton64", "Выделение вершины", WindowToolMode.SelectVertex, "\uE8B7", "select-vertex", CadToolKind.SelectVertex);
        Add(0, "SpeedButton69", "Выравнивание стеклопакетов", WindowToolMode.Other, "\uE8A8", "align-glazing", CadToolKind.AlignGlazingUnits, instant: true);
        Add(0, "SpeedButton50", "Выравнивание глухих просветов", WindowToolMode.Other, "\uE8A8", "align-dead-lights", CadToolKind.AlignDeadLights, instant: true);
        Add(0, "SpeedButton27", "Установить координаты вершины", WindowToolMode.Other, "\uE8B7", "set-vertex-coordinates", CadToolKind.SetVertexCoordinates);
        Add(0, "SpeedButton39", "Разбить профиль пополам", WindowToolMode.Other, "\uE8A6", "split-profile", CadToolKind.SplitProfile);
        Add(0, "SpeedButton17", "Сместить импост вертикально", WindowToolMode.Other, "\uE8AC", "offset-impost-v", CadToolKind.OffsetImpostVertical);
        Add(0, "SpeedButton16", "Сместить импост горизонтально", WindowToolMode.Other, "\uE8AB", "offset-impost-h", CadToolKind.OffsetImpostHorizontal);
        Add(0, "SpeedButton33", "Выравнивание импостов", WindowToolMode.Other, "\uE8A7", "align-imposts", CadToolKind.AlignImposts, instant: true);
        Add(0, "SpeedButton52", "Изменить габаритные размеры", WindowToolMode.Other, "\uE8B8", "change-dimensions", CadToolKind.ChangeOverallDimensions, instant: true);
        Add(0, "SpeedButton26", "Удаление", WindowToolMode.Other, "\uE74D", "delete-selection", CadToolKind.DeleteSelection, instant: true);
        Add(0, "SpeedButton21", "Очистить конструкцию", WindowToolMode.ClearConstruction, "\uE894", "clear-construction", CadToolKind.ClearConstruction, instant: true);
        Add(0, "SpeedButton61", "Выделение радиусных вершин", WindowToolMode.RadiusVertex, "\uE8B6", "radius-vertex", CadToolKind.RadiusVertex);
        Add(0, "SpeedButton59", "Выделение дуговых вершин", WindowToolMode.Other, "\uE8B6", "select");
        Add(0, "SpeedButton9", "Увеличить/уменьшить вид", WindowToolMode.ZoomView, "\uE8A3", "select");
        Add(0, "SpeedButton13", "Переместить вид", WindowToolMode.PanView, "\uE8C0", "select");
        Add(0, "SpeedButton14", "Автомасштаб", WindowToolMode.AutoScale, "\uE9A6", instant: true);
        Add(0, "SpeedButton25", "Свойства объекта", WindowToolMode.Other, "\uE90F", instant: true);
        Add(0, "SpeedButton43", "Пересчитать конструкцию", WindowToolMode.Other, "\uE9F5", instant: true);
        Add(0, "SpeedButton71", "Печать", WindowToolMode.Other, "\uE749", instant: true);

        // Блок 2 — базовая геометрия
        Add(1, "speedbutton15", "Коробка", WindowToolMode.Frame, "\uE7F4", "rectangle");
        Add(1, "SpeedButton40", "Коробка фиктивная", WindowToolMode.Frame, "\uE739", "design-rect");
        Add(1, "SpeedButton49", "Коробка фиктивная круглая", WindowToolMode.RoundDummyFrame, "\uECAF", "design-rect");
        Add(1, "SpeedButton18", "Добавочный элемент 1", WindowToolMode.AdditionalElement, "\uE8AB", "design-rect");
        Add(1, "SpeedButton19", "Добавочный элемент 2", WindowToolMode.AdditionalElement, "\uE8AC", "design-rect");
        Add(1, "SpeedButton22", "Притвор", WindowToolMode.Other, "\uE8A9", "design-rect");
        Add(1, "SpeedButton32", "Стойки в крайних положениях", WindowToolMode.Other, "\uE8A5", instant: true);
        Add(1, "SpeedButton47", "Линия разреза стоек", WindowToolMode.Other, "\uE8B1", instant: true);
        Add(1, "SpeedButton48", "Линия крепления стоек", WindowToolMode.Other, "\uE8B2", instant: true);
        Add(1, "SpeedButton55", "Карниз", WindowToolMode.Other, "\uE8A9", instant: true);
        Add(1, "SpeedButton56", "Типовые конструкции", WindowToolMode.Other, "\uE8B0", instant: true);
        Add(1, "SpeedButton57", "Типовые конструкции", WindowToolMode.Other, "\uE8B0", instant: true);
        Add(1, "SpeedButton67", "Арка", WindowToolMode.Other, "\uECAF", instant: true);
        Add(1, "SpeedButton68", "Арка", WindowToolMode.Other, "\uECAF", instant: true);
        Add(1, "SpeedButton72", "Задать радиус", WindowToolMode.Other, "\uE8B6", instant: true);
        Add(1, "SpeedButton60", "Убрать дуговые вершины", WindowToolMode.Other, "\uE8B6", instant: true);
        Add(1, "SpeedButton53", "Копировать конструкцию", WindowToolMode.Other, "\uE8C8", instant: true);
        Add(1, "SpeedButton54", "Вставить конструкцию", WindowToolMode.Other, "\uE8CB", instant: true);
        Add(1, "SpeedButton58", "Детали", WindowToolMode.Other, "\uE8A0", instant: true);
        Add(1, "SpeedButton20", "Установочные элементы", WindowToolMode.Other, "\uE90F", instant: true);
        Add(1, "SpeedButton42", "Установочные элементы", WindowToolMode.Other, "\uE90F", instant: true);

        // Блок 3 — импосты и стойки
        Add(2, "SpeedButton2", "Импост вертикальный", WindowToolMode.VerticalImpost, "\uE8A4", "mullion-place-v");
        Add(2, "SpeedButton4", "Импост горизонтальный", WindowToolMode.HorizontalImpost, "\uE8A3", "mullion-place-h");
        Add(2, "SpeedButton41", "Импост фиктивный вертикальный", WindowToolMode.FictitiousVerticalImpost, "\uE8A4", "mullion-place-v");
        Add(2, "SpeedButton45", "Импост фиктивный горизонтальный", WindowToolMode.FictitiousHorizontalImpost, "\uE8A3", "mullion-place-h");
        Add(2, "SpeedButton3", "Пропорц. стойки вертикальные", WindowToolMode.MullionVertical, "\uE8A6", "mullion-place-v");
        Add(2, "SpeedButton5", "Пропорц. стойки горизонтальные", WindowToolMode.MullionHorizontal, "\uE8A3", "mullion-place-h");
        Add(2, "SpeedButton6", "Стойка", WindowToolMode.Post, "\uE8A5", "mullion-place-v");
        Add(2, "SpeedButton66", "Сегмент", WindowToolMode.Other, "\uE8B1", instant: true);
        Add(2, "SpeedButton65", "Блок управления", WindowToolMode.Other, "\uE7F4", instant: true);
        Add(2, "SpeedButton23", "—", WindowToolMode.Other, "\uE8B0", instant: true);
        Add(2, "SpeedButton38", "—", WindowToolMode.Other, "\uE8B0", instant: true);

        // Блок 4 — створки и заполнения
        Add(3, "SpeedButton8", "Створка", WindowToolMode.Sash, "\uE7C3", "design-rect");
        Add(3, "SpeedButton10", "Остекление", WindowToolMode.Glass, "\uE771", "design-rect");
        Add(3, "SpeedButton70", "Остекление + штапик", WindowToolMode.GlassWithBead, "\uE771", "design-rect");
        Add(3, "SpeedButton83", "Шпросс вертикальный", WindowToolMode.ShprossVertical, "\uE8A4", "design-rect");
        Add(3, "SpeedButton84", "Шпросс горизонтальный", WindowToolMode.ShprossHorizontal, "\uE8A3", "design-rect");
        Add(3, "SpeedButton7", "Гофра", WindowToolMode.Corrugation, "\uE8A6", "design-rect");
        Add(3, "SpeedButton11", "Гофра", WindowToolMode.Corrugation, "\uE8A7", "design-rect");
        Add(3, "SpeedButton12", "Гофра", WindowToolMode.Corrugation, "\uE8A6", "design-rect");
        Add(3, "SpeedButton34", "Система штапика", WindowToolMode.BeadSystem, "\uE8B1", instant: true);
        Add(3, "SpeedButton24", "Фурнитура", WindowToolMode.Hardware, "\uE946", instant: true);
        Add(3, "SpeedButton28", "Жалюзи", WindowToolMode.Other, "\uE8A0", instant: true);
        Add(3, "SpeedButton29", "Жалюзи", WindowToolMode.Other, "\uE8A1", instant: true);
        Add(3, "SpeedButton30", "Слайдинговая створка", WindowToolMode.SlidingSash, "\uE8AB", "design-rect");
        Add(3, "SpeedButton31", "Слайдинговая створка", WindowToolMode.SlidingSash, "\uE8AC", "design-rect");
        Add(3, "SpeedButton35", "Ограничитель открывания", WindowToolMode.Other, "\uE7BA", instant: true);
        Add(3, "SpeedButton36", "Доводчик", WindowToolMode.Other, "\uE7C5", instant: true);
        Add(3, "SpeedButton37", "Шпингалет", WindowToolMode.Other, "\uE8D7", instant: true);
        Add(3, "SpeedButton51", "Шпингалет", WindowToolMode.Other, "\uE8D7", instant: true);
        Add(3, "SpeedButton73", "Балконный комплект", WindowToolMode.Other, "\uE8B0", instant: true);
        Add(3, "SpeedButton74", "Добавить конструкцию", WindowToolMode.KonAdd, "\uE710", instant: true);
        Add(3, "SpeedButton75", "Удалить конструкцию", WindowToolMode.KonDel, "\uE738", instant: true);
        Add(3, "ZkSBut1", "Заказчик", WindowToolMode.Other, "\uE716", instant: true);
        Add(3, "SpeedButton77", "Дилер", WindowToolMode.Other, "\uE716", instant: true);
        Add(3, "SpeedButton78", "Дилер", WindowToolMode.Other, "\uE716", instant: true);
        Add(3, "SpeedButton79", "Режим цены", WindowToolMode.Other, "\uE8A0", instant: true);
        Add(3, "SpeedButton76", "Режим цены", WindowToolMode.Other, "\uE8A0", instant: true);

        return items;
    }
}
