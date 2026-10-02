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
    private readonly List<StockDocumentHeader> _documents = CreateDemoDocuments();
    private int _sequence = 6;

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

    private static List<StockDocumentHeader> CreateDemoDocuments()
    {
        var minStock = new StockDocumentHeader
        {
            Number = "MIN-001",
            Date = new DateTime(2026, 1, 10),
            Type = StockDocumentType.MinStockLimit,
            Note = "Неснижаемые остатки по складу"
        };
        minStock.Lines.Add(new StockDocumentLine
        {
            MaterialId = CatalogMaterialIds.ProfileRehau70,
            Article = CatalogMaterialArticles.ProfileRehau70,
            Name = "Профиль Rehau 70 белый",
            Unit = "м/п",
            Quantity = 120m,
            Price = 0m,
            Currency = "KGS"
        });
        minStock.Lines.Add(new StockDocumentLine
        {
            MaterialId = CatalogMaterialIds.HardwareMacoEgress,
            Article = CatalogMaterialArticles.HardwareMacoEgress,
            Name = "Ножницы Maco Multi-Matic",
            Unit = "шт.",
            Quantity = 40m,
            Price = 0m,
            Currency = "KGS"
        });

        var receipt = new StockDocumentHeader
        {
            Number = "ПР-00045",
            Date = new DateTime(2026, 6, 20),
            Type = StockDocumentType.ReceiptAct,
            Note = "Поставка профиля Rehau"
        };
        receipt.Lines.Add(new StockDocumentLine
        {
            MaterialId = CatalogMaterialIds.ProfileRehau70,
            Article = CatalogMaterialArticles.ProfileRehau70,
            Name = "Профиль Rehau 70 белый",
            Color = "Белый",
            Unit = "м/п",
            Quantity = 250m,
            Price = 1250m,
            Currency = "KGS"
        });
        receipt.Lines.Add(new StockDocumentLine
        {
            MaterialId = CatalogMaterialIds.Glazing4_16_4,
            Article = CatalogMaterialArticles.Glazing4_16_4,
            Name = "Стеклопакет 4-16-4",
            Color = "Прозрачный",
            Unit = "м.кв.",
            Quantity = 36m,
            Price = 2850m,
            Currency = "KGS"
        });

        var invoice = new StockDocumentHeader
        {
            Number = "РС-00112",
            Date = new DateTime(2026, 6, 25),
            Type = StockDocumentType.Invoice,
            Note = "Отпуск на производство заказа #1024"
        };
        invoice.Lines.Add(new StockDocumentLine
        {
            MaterialId = CatalogMaterialIds.ProfileRehau70,
            Article = CatalogMaterialArticles.ProfileRehau70,
            Name = "Профиль Rehau 70 белый",
            Color = "Белый",
            Unit = "м/п",
            Quantity = 48m,
            Price = 1250m,
            Currency = "KGS"
        });

        var inventory = new StockDocumentHeader
        {
            Number = "ИНВ-0007",
            Date = new DateTime(2026, 6, 28),
            Type = StockDocumentType.InventoryAct,
            Note = "Плановая инвентаризация склада"
        };
        inventory.Lines.Add(new StockDocumentLine
        {
            MaterialId = CatalogMaterialIds.ProfileRehau70,
            Article = CatalogMaterialArticles.ProfileRehau70,
            Name = "Профиль Rehau 70 белый",
            Unit = "м/п",
            Quantity = 202m,
            Price = 1250m,
            Currency = "KGS"
        });
        inventory.Lines.Add(new StockDocumentLine
        {
            MaterialId = CatalogMaterialIds.HardwareMacoEgress,
            Article = CatalogMaterialArticles.HardwareMacoEgress,
            Name = "Ножницы Maco Multi-Matic",
            Unit = "шт.",
            Quantity = 86m,
            Price = 4200m,
            Currency = "KGS"
        });

        var request = new StockDocumentHeader
        {
            Number = "ЗВ-00018",
            Date = new DateTime(2026, 6, 29),
            Type = StockDocumentType.Request,
            Note = "Заявка на закупку фурнитуры"
        };
        request.Lines.Add(new StockDocumentLine
        {
            MaterialId = CatalogMaterialIds.HardwareMacoEgress,
            Article = CatalogMaterialArticles.HardwareMacoEgress,
            Name = "Ножницы Maco Multi-Matic",
            Unit = "шт.",
            Quantity = 20m,
            Price = 4200m,
            Currency = "KGS"
        });

        return [minStock, receipt, invoice, inventory, request];
    }
}
