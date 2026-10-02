namespace NurSapat.App.Models.Config;

public enum ConstructionSectionType
{
    Frame,
    Sash,
    SlidingSash,
    Impost,
    Glazing,
    Corrugation,
    Bead,
    Blinds,
    MosquitoNet,
    Hardware,
    Profile,
    Glass,
    Reinforcement,
    Accessories,
    Mounting,
    Handle,
    Lock,
    Limiter,
    DoorCloser,
    Latch
}

public static class ConstructionSectionTypeExtensions
{
  public static string GetDisplayName(this ConstructionSectionType section) => section switch
  {
      ConstructionSectionType.Frame => "Коробка",
      ConstructionSectionType.Sash => "Створка",
      ConstructionSectionType.SlidingSash => "Створка слайдинг",
      ConstructionSectionType.Impost => "Импост/Штульп",
      ConstructionSectionType.Glazing => "Остекление",
      ConstructionSectionType.Corrugation => "Гофра",
      ConstructionSectionType.Bead => "Штапик",
      ConstructionSectionType.Blinds => "Жалюзи",
      ConstructionSectionType.MosquitoNet => "Москитная сетка",
      ConstructionSectionType.Hardware => "Фурнитура",
      ConstructionSectionType.Profile => "Профиль",
      ConstructionSectionType.Glass => "Стекло",
      ConstructionSectionType.Reinforcement => "Армирование",
      ConstructionSectionType.Accessories => "Вспомогательные элементы",
      ConstructionSectionType.Mounting => "Монтажные элементы",
      ConstructionSectionType.Handle => "Ручка",
      ConstructionSectionType.Lock => "Замок",
      ConstructionSectionType.Limiter => "Ограничитель",
      ConstructionSectionType.DoorCloser => "Доводчик",
      ConstructionSectionType.Latch => "Шпингалет",
      _ => section.ToString()
  };

  public static bool IsTerminal(this ConstructionSectionType section) => section switch
  {
      ConstructionSectionType.Frame or
      ConstructionSectionType.Sash or
      ConstructionSectionType.SlidingSash or
      ConstructionSectionType.Impost or
      ConstructionSectionType.Glazing or
      ConstructionSectionType.Corrugation or
      ConstructionSectionType.Bead or
      ConstructionSectionType.Blinds or
      ConstructionSectionType.MosquitoNet => true,
      _ => false
  };

  public static bool UsesProfilePanel(this ConstructionSectionType section) => section switch
  {
      ConstructionSectionType.Frame or
      ConstructionSectionType.Sash or
      ConstructionSectionType.SlidingSash or
      ConstructionSectionType.Impost or
      ConstructionSectionType.Profile or
      ConstructionSectionType.Bead or
      ConstructionSectionType.Corrugation => true,
      _ => false
  };

  public static bool UsesGlazingPanel(this ConstructionSectionType section) =>
      section is ConstructionSectionType.Glazing or ConstructionSectionType.Glass;

  public static bool UsesHardwarePanel(this ConstructionSectionType section) =>
      section is ConstructionSectionType.Hardware;

  public static IReadOnlyList<ConstructionSectionType> TerminalSections { get; } =
  [
      ConstructionSectionType.Frame,
      ConstructionSectionType.Sash,
      ConstructionSectionType.SlidingSash,
      ConstructionSectionType.Impost,
      ConstructionSectionType.Glazing,
      ConstructionSectionType.Corrugation,
      ConstructionSectionType.Bead,
      ConstructionSectionType.Blinds,
      ConstructionSectionType.MosquitoNet
  ];

  public static IReadOnlyList<ConstructionSectionType> BasicSections { get; } =
  [
      ConstructionSectionType.Hardware,
      ConstructionSectionType.Profile,
      ConstructionSectionType.Glass,
      ConstructionSectionType.Reinforcement,
      ConstructionSectionType.Accessories,
      ConstructionSectionType.Mounting,
      ConstructionSectionType.Handle,
      ConstructionSectionType.Lock,
      ConstructionSectionType.Limiter,
      ConstructionSectionType.DoorCloser,
      ConstructionSectionType.Latch
  ];
}
