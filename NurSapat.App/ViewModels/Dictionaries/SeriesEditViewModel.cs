using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Dictionaries;
using NurSapat.App.Services.Dictionaries;

namespace NurSapat.App.ViewModels.Dictionaries;

public sealed partial class SeriesEditViewModel : ObservableObject
{
    private readonly IManufacturersStore _manufacturersStore;
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public SeriesEditViewModel(IManufacturersStore manufacturersStore)
    {
        _manufacturersStore = manufacturersStore;
        Manufacturers = new ObservableCollection<ManufacturerItem>();
    }

    public ObservableCollection<ManufacturerItem> Manufacturers { get; }

    public string WindowTitle => _isNew ? "Новая серия" : "Редактирование серии";

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ManufacturerItem? _selectedManufacturer;

    [ObservableProperty]
    private double _hardwareAxisMm = 13.0;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public event Action<bool>? RequestClose;

    public void Load(ProfileSeriesItem source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;

        Manufacturers.Clear();
        foreach (var manufacturer in _manufacturersStore.GetAll())
        {
            Manufacturers.Add(manufacturer);
        }

        Name = source.Name;
        HardwareAxisMm = source.HardwareAxisMm;
        SelectedManufacturer = Manufacturers.FirstOrDefault(item => item.Id == source.ManufacturerId)
            ?? Manufacturers.FirstOrDefault();
        ValidationMessage = string.Empty;

        OnPropertyChanged(nameof(WindowTitle));
    }

    public ProfileSeriesItem BuildResult() => new()
    {
        Id = _id,
        Name = Name.Trim(),
        ManufacturerId = SelectedManufacturer?.Id ?? Guid.Empty,
        HardwareAxisMm = HardwareAxisMm
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
        if (string.IsNullOrWhiteSpace(Name))
        {
            return "Укажите название серии.";
        }

        if (SelectedManufacturer is null)
        {
            return "Выберите производителя.";
        }

        if (HardwareAxisMm <= 0)
        {
            return "Ось фурнитурного паза должна быть больше нуля.";
        }

        return string.Empty;
    }
}
