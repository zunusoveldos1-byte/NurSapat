using NurSapat.Cad.Core.Document;

namespace NurSapat.Cad.Core.Snapping;

public interface ISnapProvider
{
    IEnumerable<CadSnapTick> GetSnapTicks(CadDocument document);
}
