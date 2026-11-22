using NurSapat.App.Models.Stock;

namespace NurSapat.App.Services.Stock;

public sealed record StockArticleBalance(
    string Article,
    string Name,
    string Unit,
    decimal Quantity);

public sealed record StockPeriodMovement(
    decimal ReceiptQuantity,
    decimal IssueQuantity);

public static class StockBalanceCalculator
{
    public static Dictionary<string, StockArticleBalance> CalculateBalances(
        IEnumerable<StockDocumentHeader> documents,
        DateTime? asOfDate = null)
    {
        var balances = new Dictionary<string, StockArticleBalance>(StringComparer.OrdinalIgnoreCase);
        var ordered = documents
            .Where(document => document.Type != StockDocumentType.Request)
            .Where(document => asOfDate is null || document.Date.Date <= asOfDate.Value.Date)
            .OrderBy(document => document.Date)
            .ThenBy(document => document.Number, StringComparer.OrdinalIgnoreCase);

        foreach (var document in ordered)
        {
            switch (document.Type)
            {
                case StockDocumentType.ReceiptAct:
                    ApplyReceipt(balances, document);
                    break;
                case StockDocumentType.Invoice:
                    ApplyInvoice(balances, document);
                    break;
                case StockDocumentType.InventoryAct:
                    ApplyInventory(balances, document);
                    break;
                case StockDocumentType.MinStockLimit:
                    break;
            }
        }

        return balances;
    }

    public static Dictionary<string, StockPeriodMovement> CalculateMovements(
        IEnumerable<StockDocumentHeader> documents,
        DateTime periodStart,
        DateTime periodEnd)
    {
        var movements = new Dictionary<string, StockPeriodMovement>(StringComparer.OrdinalIgnoreCase);
        var start = periodStart.Date;
        var end = periodEnd.Date;

        foreach (var document in documents)
        {
            if (document.Date.Date < start || document.Date.Date > end)
            {
                continue;
            }

            foreach (var line in document.Lines)
            {
                if (!movements.TryGetValue(line.Article, out var current))
                {
                    current = new StockPeriodMovement(0m, 0m);
                }

                movements[line.Article] = document.Type switch
                {
                    StockDocumentType.ReceiptAct => current with
                    {
                        ReceiptQuantity = current.ReceiptQuantity + line.Quantity
                    },
                    StockDocumentType.Invoice => current with
                    {
                        IssueQuantity = current.IssueQuantity + line.Quantity
                    },
                    _ => current
                };
            }
        }

        return movements;
    }

    public static Dictionary<string, decimal> GetMinStockLimits(StockDocumentHeader? minStockDocument)
    {
        var limits = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        if (minStockDocument is null)
        {
            return limits;
        }

        foreach (var line in minStockDocument.Lines)
        {
            limits[line.Article] = line.Quantity;
        }

        return limits;
    }

    private static void ApplyReceipt(Dictionary<string, StockArticleBalance> balances, StockDocumentHeader document)
    {
        foreach (var line in document.Lines)
        {
            if (balances.TryGetValue(line.Article, out var current))
            {
                balances[line.Article] = current with { Quantity = current.Quantity + line.Quantity };
            }
            else
            {
                balances[line.Article] = new StockArticleBalance(line.Article, line.Name, line.Unit, line.Quantity);
            }
        }
    }

    private static void ApplyInvoice(Dictionary<string, StockArticleBalance> balances, StockDocumentHeader document)
    {
        foreach (var line in document.Lines)
        {
            if (balances.TryGetValue(line.Article, out var current))
            {
                balances[line.Article] = current with { Quantity = current.Quantity - line.Quantity };
            }
            else
            {
                balances[line.Article] = new StockArticleBalance(line.Article, line.Name, line.Unit, -line.Quantity);
            }
        }
    }

    private static void ApplyInventory(Dictionary<string, StockArticleBalance> balances, StockDocumentHeader document)
    {
        foreach (var line in document.Lines)
        {
            balances[line.Article] = new StockArticleBalance(line.Article, line.Name, line.Unit, line.Quantity);
        }
    }
}
