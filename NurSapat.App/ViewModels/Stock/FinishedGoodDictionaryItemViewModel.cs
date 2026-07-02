using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Stock;

namespace NurSapat.App.ViewModels.Stock;

public sealed partial class FinishedGoodDictionaryItemViewModel : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _orderNumber = string.Empty;

    [ObservableProperty]
    private string _productCode = string.Empty;

    [ObservableProperty]
    private string _cellAddress = string.Empty;

    [ObservableProperty]
    private FinishedGoodState _state = FinishedGoodState.OnWarehouse;

    [ObservableProperty]
    private DateTime _dateIncoming = DateTime.Today;

    [ObservableProperty]
    private string _userName = string.Empty;

    public string StateDisplay => State.ToDisplayName();

    public string DateIncomingDisplay => DateIncoming.ToString("dd.MM.yyyy");

    partial void OnStateChanged(FinishedGoodState value) => OnPropertyChanged(nameof(StateDisplay));

    partial void OnDateIncomingChanged(DateTime value) => OnPropertyChanged(nameof(DateIncomingDisplay));

    public static FinishedGoodDictionaryItemViewModel FromModel(FinishedGoodItem item) => new()
    {
        Id = item.Id,
        OrderNumber = item.OrderNumber,
        ProductCode = item.ProductCode,
        CellAddress = item.CellAddress,
        State = item.State,
        DateIncoming = item.DateIncoming,
        UserName = item.UserName
    };

    public FinishedGoodItem ToModel() => new()
    {
        Id = Id,
        OrderNumber = OrderNumber,
        ProductCode = ProductCode,
        CellAddress = CellAddress,
        State = State,
        DateIncoming = DateIncoming,
        UserName = UserName
    };

    public void RefreshFrom(FinishedGoodItem item)
    {
        OrderNumber = item.OrderNumber;
        ProductCode = item.ProductCode;
        CellAddress = item.CellAddress;
        State = item.State;
        DateIncoming = item.DateIncoming;
        UserName = item.UserName;
    }
}
