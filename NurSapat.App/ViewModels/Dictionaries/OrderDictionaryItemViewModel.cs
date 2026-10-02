using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Orders;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class OrderDictionaryItemViewModel : DictionaryItemViewModelBase
{
    [ObservableProperty]
    private string _orderNumber = string.Empty;

    [ObservableProperty]
    private DateTime _dateCreated = DateTime.Today;

    [ObservableProperty]
    private string _customerName = string.Empty;

    [ObservableProperty]
    private int _structuresCount;

    [ObservableProperty]
    private decimal _totalSum;

    [ObservableProperty]
    private OrderStatus _status = OrderStatus.Calculation;

    [ObservableProperty]
    private string? _filePath;

    public string DateCreatedDisplay => DateCreated.ToString("dd.MM.yyyy");

    public string TotalSumDisplay => $"{TotalSum:N2}";

    public string StatusDisplay => Status.ToDisplayName();

    partial void OnDateCreatedChanged(DateTime value) => OnPropertyChanged(nameof(DateCreatedDisplay));

    partial void OnTotalSumChanged(decimal value) => OnPropertyChanged(nameof(TotalSumDisplay));

    partial void OnStatusChanged(OrderStatus value) => OnPropertyChanged(nameof(StatusDisplay));

    public static OrderDictionaryItemViewModel FromModel(OrderHeaderItem item) => new()
    {
        Id = item.Id,
        OrderNumber = item.OrderNumber,
        DateCreated = item.DateCreated,
        CustomerName = item.CustomerName,
        StructuresCount = item.StructuresCount,
        TotalSum = item.TotalSum,
        Status = item.Status,
        FilePath = item.FilePath
    };

    public OrderHeaderItem ToModel() => new()
    {
        Id = Id,
        OrderNumber = OrderNumber,
        DateCreated = DateCreated,
        CustomerName = CustomerName,
        StructuresCount = StructuresCount,
        TotalSum = TotalSum,
        Status = Status,
        FilePath = FilePath
    };

    public void RefreshFrom(OrderHeaderItem item)
    {
        OrderNumber = item.OrderNumber;
        DateCreated = item.DateCreated;
        CustomerName = item.CustomerName;
        StructuresCount = item.StructuresCount;
        TotalSum = item.TotalSum;
        Status = item.Status;
        FilePath = item.FilePath;
    }
}
