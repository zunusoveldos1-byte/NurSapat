using NurSapat.Core.Models.Materials;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Models.Construction;

/// <summary>Справочник материалов конструкций с привязкой к типам инструментов.</summary>
public static class ConstructionMaterialCatalog
{
    /// <summary>
    /// Физическая и отрисовочная толщина нащельника, мм. Единое значение для
    /// слоя рендеринга, вычисления контура и расчёта отступов светового проёма.
    /// </summary>
    public const double NashchelnikThicknessMm = 4.0;

    public static IReadOnlyList<ConstructionMaterial> All { get; } = BuildAll();

    public static ConstructionMaterial? FindById(Guid? id) =>
        id is Guid value ? All.FirstOrDefault(material => material.Id == value) : null;

    public static ConstructionMaterial? FindByArticle(string? article) =>
        string.IsNullOrWhiteSpace(article)
            ? null
            : All.FirstOrDefault(material =>
                string.Equals(material.CatalogArticle, article, StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<ConstructionMaterial> ForKind(GeometryElementKind kind) =>
        kind switch
        {
            GeometryElementKind.EdgeMullion =>
                // Крайняя стойка/ригель ставится обычным импостным профилем либо
                // специальным накладным (нащельник, 65 рама НИЗ).
                All.Where(material => material.ApplicableKind
                    is GeometryElementKind.EdgeMullion
                    or GeometryElementKind.Mullion),
            // Фурнитура: петли, механизмы и ручки в одном отфильтрованном списке.
            GeometryElementKind.Hardware =>
                All.Where(material => material.ApplicableKind
                    is GeometryElementKind.Hardware
                    or GeometryElementKind.Handle),
            GeometryElementKind.Handle =>
                All.Where(material => material.ApplicableKind
                    is GeometryElementKind.Handle
                    or GeometryElementKind.Hardware),
            _ => All.Where(material => material.ApplicableKind == kind)
        };

    /// <summary>Проверяет, что у всех материалов каталога задан артикул.</summary>
    public static void EnsureAllHaveArticles()
    {
        var missing = All.Where(material => string.IsNullOrWhiteSpace(material.CatalogArticle)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Construction materials without CatalogArticle: {string.Join(", ", missing.Select(m => m.Name))}");
        }
    }

    private static IReadOnlyList<ConstructionMaterial> BuildAll()
    {
        var materials = new List<ConstructionMaterial>
        {
        // Рама / коробка
        Material("b1000001-0000-4000-8000-000000000001", CatalogMaterialArticles.ProfileRehau70, "Профиль рамный ПВХ (Белый)", GeometryElementKind.Frame, 68, 0, EndCutType.Mitre45),
        Material("b1000002-0000-4000-8000-000000000002", CatalogMaterialArticles.ProfileRehau70, "Профиль рамный ПВХ (Ламинация)", GeometryElementKind.Frame, 68, 0, EndCutType.Mitre45),
        Material("b1000003-0000-4000-8000-000000000003", CatalogMaterialArticles.ProfileRehau70, "Профиль Алюминий рамный", GeometryElementKind.Frame, 70, 0, EndCutType.Mitre45),

        // Круглая рама
        Material("b1000010-0000-4000-8000-000000000010", CatalogMaterialArticles.ProfileRehau70, "ПВХ Профиль Гнутый (Белый)", GeometryElementKind.RoundDummyFrame, 68, 0, EndCutType.ButtJoint0),

        // Импост
        Material("b1000020-0000-4000-8000-000000000020", CatalogMaterialArticles.ProfileRehau70, "Импост стандартный ПВХ", GeometryElementKind.Mullion, 68, 0, EndCutType.ImpostMilling),
        Material("b1000021-0000-4000-8000-000000000021", CatalogMaterialArticles.ProfileRehau70, "Импост усиленный (статически мощный)", GeometryElementKind.Mullion, 92, 0, EndCutType.ImpostMilling),

        // Стойка/ригель в крайнем положении — тонкие накладные профили.
        // Строятся обычным алгоритмом инструмента, но на чертеже отображаются
        // узкой полосой (DrawThicknessMm), как реальный нащельник / нижняя рама 65.
        // Толщина нащельника = LightOpeningInsetMm = 4 мм: переходной профиль
        // упирается ровно в его грань без зазоров (см. NashchelnikThicknessMm).
        Material("b1000090-0000-4000-8000-000000000090", CatalogMaterialArticles.ProfileRehau70, "Нащельник", GeometryElementKind.EdgeMullion, 40, 100, EndCutType.ButtJoint0, drawThicknessMm: NashchelnikThicknessMm, snapsToConstructionAxes: true, lightOpeningInsetMm: NashchelnikThicknessMm),
        Material("b1000091-0000-4000-8000-000000000091", CatalogMaterialArticles.ProfileRehau70, "65 рама НИЗ", GeometryElementKind.EdgeMullion, 65, 0, EndCutType.ButtJoint0, drawThicknessMm: 2.5),

        // Створка
        Material("b1000030-0000-4000-8000-000000000030", CatalogMaterialArticles.ProfileRehau70, "Профиль створочный Z-образный", GeometryElementKind.Sash, 68, 0, EndCutType.Mitre45),
        Material("b1000031-0000-4000-8000-000000000031", CatalogMaterialArticles.ProfileRehau70, "Профиль створочный Т-образный", GeometryElementKind.Sash, 70, 0, EndCutType.Mitre45),

        // Фурнитура (петли / ручки)
        Material("b1000100-0000-4000-8000-000000000100", CatalogMaterialArticles.HardwareHingeWindowWhite, "Петля оконная ПВХ (Белая)", GeometryElementKind.Hardware, 0, 0, EndCutType.ButtJoint0),
        Material("b1000101-0000-4000-8000-000000000101", CatalogMaterialArticles.HardwareHingeDoorReinforced, "Петля усиленная дверная", GeometryElementKind.Hardware, 0, 0, EndCutType.ButtJoint0),
        Material("b1000102-0000-4000-8000-000000000102", CatalogMaterialArticles.HardwareHandleWindowStd, "Ручка оконная стандартная", GeometryElementKind.Handle, 0, 0, EndCutType.ButtJoint0),
        Material("b1000103-0000-4000-8000-000000000103", CatalogMaterialArticles.HardwareMacoEgress, "Фурнитура MACO Egress", GeometryElementKind.Hardware, 0, 0, EndCutType.ButtJoint0),

        // Штапик
        Material("b1000040-0000-4000-8000-000000000040", CatalogMaterialArticles.Bead24, "Штапик под 24 мм стеклопакет", GeometryElementKind.Bead, 18, 0, EndCutType.Mitre45),
        Material("b1000041-0000-4000-8000-000000000041", CatalogMaterialArticles.Bead24, "Штапик под 32 мм стеклопакет", GeometryElementKind.Bead, 22, 0, EndCutType.Mitre45),

        // Арочная рама
        Material("b1000050-0000-4000-8000-000000000050", CatalogMaterialArticles.ProfileRehau70, "Профиль рамный арочный ПВХ", GeometryElementKind.ArchFrame, 68, 0, EndCutType.Mitre45),

        // Переходный профиль
        Material("b1000060-0000-4000-8000-000000000060", CatalogMaterialArticles.ProfileRehau70, "Переходной профиль (адаптер)", GeometryElementKind.TransitionFrame, 46, 0, EndCutType.Mitre45),
        Material("b1000061-0000-4000-8000-000000000061", CatalogMaterialArticles.ProfilePvc65Window, "65 оконная рама", GeometryElementKind.TransitionFrame, 65, 0, EndCutType.Mitre45),
        Material("b1000062-0000-4000-8000-000000000062", CatalogMaterialArticles.ProfilePvc65CoplanarT, "65 компланарная Т (для двери)", GeometryElementKind.TransitionFrame, 65, 0, EndCutType.Mitre45, spansFullConstructionHeight: true),

        // Декоративные шпросы
        Material("b1000070-0000-4000-8000-000000000070", CatalogMaterialArticles.Spoke8White, "Шпрос 8 мм (белый)", GeometryElementKind.DecorativeSpoke, 8, 80, EndCutType.ButtJoint0),
        Material("b1000071-0000-4000-8000-000000000071", CatalogMaterialArticles.Spoke8Gold, "Шпрос 8 мм (золото)", GeometryElementKind.DecorativeSpoke, 8, 80, EndCutType.ButtJoint0),
        Material("b1000072-0000-4000-8000-000000000072", CatalogMaterialArticles.Spoke8Brown, "Шпрос 8 мм (коричневый)", GeometryElementKind.DecorativeSpoke, 8, 80, EndCutType.ButtJoint0),
        Material("b1000073-0000-4000-8000-000000000073", CatalogMaterialArticles.Spoke18White, "Шпрос 18 мм (белый)", GeometryElementKind.DecorativeSpoke, 18, 180, EndCutType.ButtJoint0),
        Material("b1000074-0000-4000-8000-000000000074", CatalogMaterialArticles.Spoke18Gold, "Шпрос 18 мм (золото)", GeometryElementKind.DecorativeSpoke, 18, 180, EndCutType.ButtJoint0),
        Material("b1000075-0000-4000-8000-000000000075", CatalogMaterialArticles.Spoke18Brown, "Шпрос 18 мм (коричневый)", GeometryElementKind.DecorativeSpoke, 18, 180, EndCutType.ButtJoint0),
        Material("b1000076-0000-4000-8000-000000000076", CatalogMaterialArticles.Spoke26White, "Шпрос 26 мм (белый)", GeometryElementKind.DecorativeSpoke, 26, 260, EndCutType.ButtJoint0),
        Material("b1000077-0000-4000-8000-000000000077", CatalogMaterialArticles.Spoke26Gold, "Шпрос 26 мм (золото)", GeometryElementKind.DecorativeSpoke, 26, 260, EndCutType.ButtJoint0),
        Material("b1000078-0000-4000-8000-000000000078", CatalogMaterialArticles.Spoke26Brown, "Шпрос 26 мм (коричневый)", GeometryElementKind.DecorativeSpoke, 26, 260, EndCutType.ButtJoint0),

        // Заполнение и аксессуары
        Material("b1000085-0000-4000-8000-000000000085", CatalogMaterialArticles.Glazing4_16_4, "Стеклопакет 4-16-4 (однокамерный)", GeometryElementKind.Glazing, 24, 24, EndCutType.ButtJoint0),
        Material("b1000086-0000-4000-8000-000000000086", CatalogMaterialArticles.Glazing4_16_4, "Стеклопакет 4-10-4-10-4 (двухкамерный)", GeometryElementKind.Glazing, 32, 32, EndCutType.ButtJoint0),
        Material("b1000080-0000-4000-8000-000000000080", CatalogMaterialArticles.Glazing4_16_4, "Гофра алюминиевая", GeometryElementKind.Corrugation, 18, 18, EndCutType.ButtJoint0),
        Material("b1000081-0000-4000-8000-000000000081", CatalogMaterialArticles.MosquitoNetFiberglass, "Москитная сетка Fiberglass", GeometryElementKind.MosquitoNet, 1.2, 1.2, EndCutType.ButtJoint0),
        Material("b1000082-0000-4000-8000-000000000082", CatalogMaterialArticles.SillPvc300, "Подоконник ПВХ 300 мм", GeometryElementKind.Sill, 30, 900, EndCutType.ButtJoint0),
        Material("b1000084-0000-4000-8000-000000000084", CatalogMaterialArticles.SillAluWhite, "Белый (Алюминий)", GeometryElementKind.Sill, 30, 900, EndCutType.ButtJoint0),
        Material("b1000083-0000-4000-8000-000000000083", CatalogMaterialArticles.ProfileRehau70, "Профиль рамный треугольный ПВХ", GeometryElementKind.TriangleDummyFrame, 68, 0, EndCutType.Mitre45)
        };

        var missing = materials.Where(material => string.IsNullOrWhiteSpace(material.CatalogArticle)).ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Construction materials without CatalogArticle: {string.Join(", ", missing.Select(m => m.Name))}");
        }

        return materials;
    }

    private static ConstructionMaterial Material(
        string id,
        string catalogArticle,
        string name,
        GeometryElementKind kind,
        double profileWidth,
        double crossSectionArea,
        EndCutType endCutType,
        double? drawThicknessMm = null,
        bool snapsToConstructionAxes = false,
        double? lightOpeningInsetMm = null,
        bool spansFullConstructionHeight = false) =>
        new()
        {
            Id = Guid.Parse(id),
            CatalogArticle = catalogArticle,
            Name = name,
            ApplicableKind = kind,
            ProfileWidth = profileWidth,
            CrossSectionArea = crossSectionArea,
            EndCutType = endCutType,
            DrawThicknessMm = drawThicknessMm,
            SnapsToConstructionAxes = snapsToConstructionAxes,
            LightOpeningInsetMm = lightOpeningInsetMm,
            SpansFullConstructionHeight = spansFullConstructionHeight
        };
}
