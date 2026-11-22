using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class ExchangeRateEditViewModel : ObservableObject
{
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public string WindowTitle => _isNew ? "Новый пакет курсов" : "Редактирование курсов валют";

    [ObservableProperty]
    private DateTimeOffset? _startDate = DateTimeOffset.Now;

    [ObservableProperty]
    private decimal _usdRate = 87.45m;

    [ObservableProperty]
    private decimal _eurRate = 99.62m;

    [ObservableProperty]
    private decimal _cnyRate = 12.88m;

    [ObservableProperty]
    private decimal _ueRate = 87.45m;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(ExchangeRatePackage source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;

        StartDate = new DateTimeOffset(source.StartDate.Date);
        UsdRate = source.UsdRate;
        EurRate = source.EurRate;
        CnyRate = source.CnyRate;
        UeRate = source.UeRate;
        ValidationMessage = string.Empty;

        OnPropertyChanged(nameof(WindowTitle));
    }

    public ExchangeRatePackage BuildResult() => new()
    {
        Id = _id,
        StartDate = StartDate?.DateTime.Date ?? DateTime.Today,
        UsdRate = UsdRate,
        EurRate = EurRate,
        CnyRate = CnyRate,
        UeRate = UeRate
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
        if (UsdRate <= 0 || EurRate <= 0 || CnyRate <= 0 || UeRate <= 0)
        {
            return "Все курсы должны быть больше нуля.";
        }

        return string.Empty;
    }
}
