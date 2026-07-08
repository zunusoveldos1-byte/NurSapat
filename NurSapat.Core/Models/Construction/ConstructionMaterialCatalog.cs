using NurSapat.Core.Models.Orders;

namespace NurSapat.Core.Models.Construction;

/// <summary>Справочник материалов конструкций с привязкой к типам инструментов.</summary>
public static class ConstructionMaterialCatalog
{
    public static IReadOnlyList<ConstructionMaterial> All { get; } = BuildAll();

    public static ConstructionMaterial? FindById(Guid? id) =>
        id is Guid value ? All.FirstOrDefault(material => material.Id == value) : null;

    public static IEnumerable<ConstructionMaterial> ForKind(GeometryElementKind kind) =>
        All.Where(material => material.ApplicableKind == kind);

    private static IReadOnlyList<ConstructionMaterial> BuildAll() =>
    [
        // Рама / коробка
        Material("b1000001-0000-4000-8000-000000000001", "Профиль рамный ПВХ (Белый)", GeometryElementKind.Frame, 68, 0, EndCutType.Mitre45),
        Material("b1000002-0000-4000-8000-000000000002", "Профиль рамный ПВХ (Ламинация)", GeometryElementKind.Frame, 68, 0, EndCutType.Mitre45),
        Material("b1000003-0000-4000-8000-000000000003", "Профиль Алюминий рамный", GeometryElementKind.Frame, 70, 0, EndCutType.Mitre45),

        // Круглая рама
        Material("b1000010-0000-4000-8000-000000000010", "ПВХ Профиль Гнутый (Белый)", GeometryElementKind.RoundDummyFrame, 68, 0, EndCutType.ButtJoint0),

        // Импост
        Material("b1000020-0000-4000-8000-000000000020", "Импост стандартный ПВХ", GeometryElementKind.Mullion, 68, 0, EndCutType.ImpostMilling),
        Material("b1000021-0000-4000-8000-000000000021", "Импост усиленный (статически мощный)", GeometryElementKind.Mullion, 92, 0, EndCutType.ImpostMilling),

        // Створка
        Material("b1000030-0000-4000-8000-000000000030", "Профиль створочный Z-образный", GeometryElementKind.Sash, 68, 0, EndCutType.Mitre45),
        Material("b1000031-0000-4000-8000-000000000031", "Профиль створочный Т-образный", GeometryElementKind.Sash, 70, 0, EndCutType.Mitre45),

        // Штапик
        Material("b1000040-0000-4000-8000-000000000040", "Штапик под 24 мм стеклопакет", GeometryElementKind.Bead, 18, 0, EndCutType.Mitre45),
        Material("b1000041-0000-4000-8000-000000000041", "Штапик под 32 мм стеклопакет", GeometryElementKind.Bead, 22, 0, EndCutType.Mitre45),

        // Арочная рама
        Material("b1000050-0000-4000-8000-000000000050", "Профиль рамный арочный ПВХ", GeometryElementKind.ArchFrame, 68, 0, EndCutType.Mitre45),

        // Переходный профиль
        Material("b1000060-0000-4000-8000-000000000060", "Переходный профиль (адаптер)", GeometryElementKind.TransitionFrame, 46, 0, EndCutType.Mitre45),

        // Декоративные шпросы
        Material("b1000070-0000-4000-8000-000000000070", "Шпрос 8 мм (белый)", GeometryElementKind.DecorativeSpoke, 8, 80, EndCutType.ButtJoint0),
        Material("b1000071-0000-4000-8000-000000000071", "Шпрос 8 мм (золото)", GeometryElementKind.DecorativeSpoke, 8, 80, EndCutType.ButtJoint0),
        Material("b1000072-0000-4000-8000-000000000072", "Шпрос 8 мм (коричневый)", GeometryElementKind.DecorativeSpoke, 8, 80, EndCutType.ButtJoint0),
        Material("b1000073-0000-4000-8000-000000000073", "Шпрос 18 мм (белый)", GeometryElementKind.DecorativeSpoke, 18, 180, EndCutType.ButtJoint0),
        Material("b1000074-0000-4000-8000-000000000074", "Шпрос 18 мм (золото)", GeometryElementKind.DecorativeSpoke, 18, 180, EndCutType.ButtJoint0),
        Material("b1000075-0000-4000-8000-000000000075", "Шпрос 18 мм (коричневый)", GeometryElementKind.DecorativeSpoke, 18, 180, EndCutType.ButtJoint0),
        Material("b1000076-0000-4000-8000-000000000076", "Шпрос 26 мм (белый)", GeometryElementKind.DecorativeSpoke, 26, 260, EndCutType.ButtJoint0),
        Material("b1000077-0000-4000-8000-000000000077", "Шпрос 26 мм (золото)", GeometryElementKind.DecorativeSpoke, 26, 260, EndCutType.ButtJoint0),
        Material("b1000078-0000-4000-8000-000000000078", "Шпрос 26 мм (коричневый)", GeometryElementKind.DecorativeSpoke, 26, 260, EndCutType.ButtJoint0),

        // Заполнение и аксессуары
        Material("b1000080-0000-4000-8000-000000000080", "Гофра алюминиевая", GeometryElementKind.Corrugation, 18, 18, EndCutType.ButtJoint0),
        Material("b1000081-0000-4000-8000-000000000081", "Москитная сетка Fiberglass", GeometryElementKind.MosquitoNet, 1.2, 1.2, EndCutType.ButtJoint0),
        Material("b1000082-0000-4000-8000-000000000082", "Подоконник ПВХ 300 мм", GeometryElementKind.Sill, 30, 900, EndCutType.ButtJoint0),
        Material("b1000084-0000-4000-8000-000000000084", "Белый (Алюминий)", GeometryElementKind.Sill, 30, 900, EndCutType.ButtJoint0),
        Material("b1000083-0000-4000-8000-000000000083", "Профиль рамный треугольный ПВХ", GeometryElementKind.TriangleDummyFrame, 68, 0, EndCutType.Mitre45)
    ];

    private static ConstructionMaterial Material(
        string id,
        string name,
        GeometryElementKind kind,
        double profileWidth,
        double crossSectionArea,
        EndCutType endCutType) =>
        new()
        {
            Id = Guid.Parse(id),
            Name = name,
            ApplicableKind = kind,
            ProfileWidth = profileWidth,
            CrossSectionArea = crossSectionArea,
            EndCutType = endCutType
        };
}
