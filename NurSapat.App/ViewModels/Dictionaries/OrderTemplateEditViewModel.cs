using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class OrderTemplateEditViewModel : ObservableObject
{
    private readonly IManufacturersStore _manufacturersStore;
    private readonly IAppMessageService _messageService;
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public OrderTemplateEditViewModel(
        IManufacturersStore manufacturersStore,
        IAppMessageService messageService)
    {
        _manufacturersStore = manufacturersStore;
        _messageService = messageService;
        ManufacturerOptions = new ObservableCollection<ManufacturerSelectionOption>();
        CurrencyOptions = ["KGS", "USD", "EUR"];
    }

    public ObservableCollection<ManufacturerSelectionOption> ManufacturerOptions { get; }

    public IReadOnlyList<string> CurrencyOptions { get; }

    public string WindowTitle => _isNew ? "Новый шаблон заказа" : "Редактирование шаблона заказа";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ManufacturerSelectionOption? _selectedManufacturerOption;

    [ObservableProperty]
    private double _windPressure;

    [ObservableProperty]
    private decimal _profitMargin;

    [ObservableProperty]
    private double _additionalExpensesPercent;

    [ObservableProperty]
    private decimal _additionalExpensesFixed;

    [ObservableProperty]
    private decimal _dismantlingPrice;

    [ObservableProperty]
    private decimal _installationPrice;

    [ObservableProperty]
    private string _currencyCode = "KGS";

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(OrderTemplateItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;

        ManufacturerOptions.Clear();
        ManufacturerOptions.Add(ManufacturerSelectionOption.All);
        foreach (var manufacturer in _manufacturersStore.GetAll())
        {
            ManufacturerOptions.Add(ManufacturerSelectionOption.FromManufacturer(manufacturer.Id, manufacturer.Name));
        }

        Name = source.Name;
        SelectedManufacturerOption = source.ManufacturerId is null
            ? ManufacturerSelectionOption.All
            : ManufacturerOptions.FirstOrDefault(option => option.ManufacturerId == source.ManufacturerId)
              ?? ManufacturerSelectionOption.All;
        WindPressure = source.WindPressure;
        ProfitMargin = source.ProfitMargin;
        AdditionalExpensesPercent = source.AdditionalExpensesPercent;
        AdditionalExpensesFixed = source.AdditionalExpensesFixed;
        DismantlingPrice = source.DismantlingPrice;
        InstallationPrice = source.InstallationPrice;
        CurrencyCode = source.CurrencyCode;
        ValidationMessage = string.Empty;

        OnPropertyChanged(nameof(WindowTitle));
    }

    public OrderTemplateItem BuildResult() => new()
    {
        Id = _id,
        Name = Name.Trim(),
        ManufacturerId = SelectedManufacturerOption?.ManufacturerId,
        WindPressure = WindPressure,
        ProfitMargin = ProfitMargin,
        AdditionalExpensesPercent = AdditionalExpensesPercent,
        AdditionalExpensesFixed = AdditionalExpensesFixed,
        DismantlingPrice = DismantlingPrice,
        InstallationPrice = InstallationPrice,
        CurrencyCode = CurrencyCode
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

    [RelayCommand]
    private void OpenTemplateParameters()
    {
        _messageService.LogInfo("Диалог шаблонных параметров заказа будет доступен в следующей версии.");
    }

    private string Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Укажите наименование шаблона.";
        }

        if (string.IsNullOrWhiteSpace(CurrencyCode))
        {
            return "Выберите валюту шаблона.";
        }

        return string.Empty;
    }
}
