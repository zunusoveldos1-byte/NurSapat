using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Stock;

namespace NurSapat.App.ViewModels.Stock;

public sealed partial class StockDocumentItemViewModel : ObservableObject
{
    public Guid Id { get; init; }

    [ObservableProperty]
    private string _number = string.Empty;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private StockDocumentType _type = StockDocumentType.ReceiptAct;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private decimal _documentTotalSum;

    public string DateDisplay => Date.ToString("dd.MM.yyyy");

    public string TypeDisplay => StockDocumentTypeLabels.GetDisplay(Type);

    public string TotalSumDisplay => $"{DocumentTotalSum:N2}";

    partial void OnDateChanged(DateTime value) => OnPropertyChanged(nameof(DateDisplay));

    partial void OnTypeChanged(StockDocumentType value) => OnPropertyChanged(nameof(TypeDisplay));

    partial void OnDocumentTotalSumChanged(decimal value) => OnPropertyChanged(nameof(TotalSumDisplay));

    public static StockDocumentItemViewModel FromModel(StockDocumentHeader document) => new()
    {
        Id = document.Id,
        Number = document.Number,
        Date = document.Date,
        Type = document.Type,
        Note = document.Note,
        DocumentTotalSum = document.DocumentTotalSum
    };

    public void RefreshFrom(StockDocumentHeader document)
    {
        Number = document.Number;
        Date = document.Date;
        Type = document.Type;
        Note = document.Note;
        DocumentTotalSum = document.DocumentTotalSum;
    }
}
