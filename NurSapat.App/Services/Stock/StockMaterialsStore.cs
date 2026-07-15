using NurSapat.App.Models.Stock;
using NurSapat.Core.Models.Materials;

namespace NurSapat.App.Services.Stock;

public interface IStockMaterialsStore
{
    IReadOnlyList<StockDocumentHeader> GetAll();

    StockDocumentHeader? GetById(Guid id);

    StockDocumentHeader? GetMinStockLimitDocument();

    void Add(StockDocumentHeader document);

    void Update(StockDocumentHeader document);

    void Delete(Guid id);

    int GetNextSequence();
}

public sealed class StockMaterialsStore : IStockMaterialsStore
{
    private readonly List<StockDocumentHeader> _documents = [];
    private int _sequence = 1;

    public IReadOnlyList<StockDocumentHeader> GetAll() =>
        _documents.OrderByDescending(document => document.Date).ThenByDescending(document => document.Number).ToList();

    public StockDocumentHeader? GetById(Guid id) =>
        _documents.FirstOrDefault(document => document.Id == id);

    public StockDocumentHeader? GetMinStockLimitDocument() =>
        _documents.FirstOrDefault(document => document.Type == StockDocumentType.MinStockLimit);

    public void Add(StockDocumentHeader document) => _documents.Add(document);

    public void Update(StockDocumentHeader document)
    {
        var index = _documents.FindIndex(item => item.Id == document.Id);
        if (index >= 0)
        {
            _documents[index] = document;
        }
    }

    public void Delete(Guid id) => _documents.RemoveAll(document => document.Id == id);

    public int GetNextSequence() => ++_sequence;

    private static List<StockDocumentHeader> CreateDemoDocuments() => [];
}
