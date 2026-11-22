using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Orders;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class OrderHeaderEditViewModel : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public string WindowTitle => _isNew ? "Новый заказ" : "Редактирование заказа";

    public IReadOnlyList<OrderStatus> StatusOptions { get; } = Enum.GetValues<OrderStatus>();

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
    private OrderStatus _status = OrderStatus.Calculated;

    [ObservableProperty]
    private string? _filePath;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(OrderHeaderItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        OrderNumber = source.OrderNumber;
        DateCreated = source.DateCreated;
        CustomerName = source.CustomerName;
        StructuresCount = source.StructuresCount;
        TotalSum = source.TotalSum;
        Status = source.Status;
        FilePath = source.FilePath;
        ValidationMessage = string.Empty;
        OnPropertyChanged(nameof(WindowTitle));
    }

    public OrderHeaderItem BuildResult() => new()
    {
        Id = _id,
        OrderNumber = OrderNumber.Trim(),
        DateCreated = DateCreated,
        CustomerName = CustomerName.Trim(),
        StructuresCount = Math.Max(0, StructuresCount),
        TotalSum = TotalSum,
        Status = Status,
        FilePath = FilePath
    };

    [RelayCommand]
    private void Save()
    {
        ValidationMessage = Validate();
        if (!string.IsNullOrEmpty(ValidationMessage))
        {
            return;
        }

        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    private string Validate()
    {
        if (string.IsNullOrWhiteSpace(OrderNumber))
        {
            return "Укажите номер заказа.";
        }

        if (string.IsNullOrWhiteSpace(CustomerName))
        {
            return "Укажите заказчика.";
        }

        return string.Empty;
    }
}
