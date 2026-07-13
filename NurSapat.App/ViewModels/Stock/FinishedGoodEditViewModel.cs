using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Stock;
using NurSapat.App.Services;
using NurSapat.App.Services.Stock;
using NurSapat.App.Services.Users;

namespace NurSapat.App.ViewModels.Stock;

public sealed partial class FinishedGoodEditViewModel : ObservableObject
{
    private readonly IUserSessionService _sessionService;
    private Guid _id = Guid.NewGuid();
    private Guid _orderId;
    private bool _isNew;

    public FinishedGoodEditViewModel(IUserSessionService sessionService)
    {
        _sessionService = sessionService;
        StateOptions = FinishedGoodStateOption.All;
    }

    public IReadOnlyList<FinishedGoodStateOption> StateOptions { get; }

    public string WindowTitle => _isNew ? "Приём на склад" : "Редактирование записи";

    [ObservableProperty]
    private string _orderNumber = string.Empty;

    [ObservableProperty]
    private string _productCode = string.Empty;

    [ObservableProperty]
    private string _cellAddress = string.Empty;

    [ObservableProperty]
    private FinishedGoodStateOption? _selectedStateOption;

    [ObservableProperty]
    private DateTimeOffset? _dateIncoming = DateTimeOffset.Now;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(FinishedGoodItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;
        _orderId = source.OrderId;

        OrderNumber = source.OrderNumber;
        ProductCode = source.ProductCode;
        CellAddress = source.CellAddress;
        SelectedStateOption = StateOptions.FirstOrDefault(option => option.Value == source.State)
            ?? StateOptions.First();
        DateIncoming = new DateTimeOffset(source.DateIncoming.Date);
        UserName = isNew
            ? _sessionService.CurrentUser?.Username ?? "—"
            : source.UserName;
        ValidationMessage = string.Empty;

        OnPropertyChanged(nameof(WindowTitle));
    }

    public FinishedGoodItem BuildResult() => new()
    {
        Id = _id,
        OrderId = _orderId,
        OrderNumber = OrderNumber.Trim(),
        ProductCode = ProductCode.Trim(),
        CellAddress = CellAddress.Trim(),
        State = SelectedStateOption?.Value ?? FinishedGoodState.OnWarehouse,
        DateIncoming = DateIncoming?.DateTime.Date ?? DateTime.Today,
        UserName = UserName
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

        if (string.IsNullOrWhiteSpace(ProductCode))
        {
            return "Укажите код продукции.";
        }

        if (string.IsNullOrWhiteSpace(CellAddress))
        {
            return "Укажите ячейку хранения.";
        }

        return string.Empty;
    }
}
