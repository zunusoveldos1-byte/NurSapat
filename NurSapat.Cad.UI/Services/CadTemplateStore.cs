using NurSapat.Cad.UI.Models;
using System.Collections.ObjectModel;

namespace NurSapat.Cad.UI.Services;

public sealed class CadTemplateStore
{
    public ObservableCollection<CadDrawingTemplate> Templates { get; } = [];

    public CadTemplateStore()
    {
        Templates.Add(new CadDrawingTemplate { Name = "Пустой чертеж", WidthMm = 0, HeightMm = 0 });
        Templates.Add(new CadDrawingTemplate { Name = "Стандарт 1200×1400", WidthMm = 1200, HeightMm = 1400 });
        Templates.Add(new CadDrawingTemplate { Name = "Окно 1000×1200", WidthMm = 1000, HeightMm = 1200 });
        Templates.Add(new CadDrawingTemplate { Name = "Фиктивная 800×1000", WidthMm = 800, HeightMm = 1000, IsFakeFrame = true });
    }
}
