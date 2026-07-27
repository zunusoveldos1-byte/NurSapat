using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.App.ViewModels.Materials;

public sealed partial class MaterialColorAddDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _colorArticle = string.Empty;

    [ObservableProperty]
    private string _selectedColorName = "Белый";

    [ObservableProperty]
    private decimal _price;

    [ObservableProperty]
    private string _linkArticle = string.Empty;

    [ObservableProperty]
    private bool _useCustomCutLength;

    [ObservableProperty]
    private decimal _customBarLengthMm = 6500;

    public IReadOnlyList<string> CatalogColors { get; } =
    [
        "Белый",
        "Коричневый",
        "Антрацит",
        "Золотой дуб",
        "Махагон"
    ];

    public static IReadOnlyDictionary<string, string> ColorHexMap { get; } =
        new Dictionary<string, string>
        {
            ["Белый"] = "#FFFFFF",
            ["Коричневый"] = "#8B4513",
            ["Антрацит"] = "#4A4A4A",
            ["Золотой дуб"] = "#C8A951",
            ["Махагон"] = "#6B2F1A"
        };

    [RelayCommand]
    private void Confirm()
    {
        Confirmed = true;
        RequestClose?.Invoke(true);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    public bool Confirmed { get; private set; }

    public event Action<bool>? RequestClose;

    public MaterialColorPriceViewModel BuildRow() => new()
    {
        ColorArticle = ColorArticle,
        ColorName = SelectedColorName,
        ColorHex = ColorHexMap.TryGetValue(SelectedColorName, out var hex) ? hex : "#FFFFFF",
        Price = Price,
        LinkArticle = LinkArticle,
        UseCustomCutLength = UseCustomCutLength,
        CustomBarLengthMm = UseCustomCutLength ? CustomBarLengthMm : null
    };
}
