using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.Materials;
using NurSapat.App.Models.Stock;
using NurSapat.App.Services.Materials;
using NurSapat.App.Services.Stock;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Stock;

public sealed partial class StockDocumentEditViewModel : ObservableObject
{
    private readonly IMaterialPickerService _materialPickerService;
    private Guid _id = Guid.NewGuid();
    private bool _isNew;

    public StockDocumentEditViewModel(IMaterialPickerService materialPickerService)
    {
        _materialPickerService = materialPickerService;
        Currencies = Enum.GetValues<MaterialCurrency>();
    }

    public string WindowTitle => _isNew ? "Новый складской документ" : "Редактирование документа";

    public IReadOnlyList<StockDocumentTypeOption> DocumentTypeOptions { get; } = StockDocumentTypeOption.CreateAll();

    public static IReadOnlyList<string> CurrencyCodes { get; } = ["KGS", "USD", "EUR", "CNY"];

    public IReadOnlyList<MaterialCurrency> Currencies { get; }

    [ObservableProperty]
    private StockDocumentTypeOption? _selectedDocumentTypeOption;

    public ObservableCollection<StockDocumentLine> Lines { get; } = [];

    public Avalonia.Controls.Window? OwnerWindow { get; set; }

    [ObservableProperty]
    private string _number = string.Empty;

    [ObservableProperty]
    private DateTime _date = DateTime.Today;

    [ObservableProperty]
    private StockDocumentType _type = StockDocumentType.ReceiptAct;

    [ObservableProperty]
    private string _note = string.Empty;

    [ObservableProperty]
    private bool _useCustomExchangeRates;

    [ObservableProperty]
    private StockDocumentLine? _selectedLine;

    [ObservableProperty]
    private bool _isLineEditorVisible;

    public decimal DocumentTotalSum => Lines.Sum(line => line.TotalSum);

    public event Action<bool>? RequestClose;

    public void Load(StockDocumentHeader source, bool isNew)
    {
        _isNew = isNew;
        _id = isNew ? Guid.NewGuid() : source.Id;

        Number = source.Number;
        Date = source.Date;
        Type = source.Type;
        SelectedDocumentTypeOption = DocumentTypeOptions.FirstOrDefault(option => option.Value == source.Type);
        Note = source.Note;
        UseCustomExchangeRates = source.UseCustomExchangeRates;

        Lines.Clear();
        foreach (var line in source.Lines)
        {
            var clone = line.Clone();
            clone.PropertyChanged += (_, _) => OnPropertyChanged(nameof(DocumentTotalSum));
            Lines.Add(clone);
        }

        SelectedLine = Lines.FirstOrDefault();
        IsLineEditorVisible = SelectedLine is not null;
        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(DocumentTotalSum));
    }

    public StockDocumentHeader BuildResult()
    {
        var document = new StockDocumentHeader
        {
            Id = _id,
            Number = Number.Trim(),
            Date = Date,
            Type = Type,
            Note = Note.Trim(),
            UseCustomExchangeRates = UseCustomExchangeRates
        };

        foreach (var line in Lines)
        {
            document.Lines.Add(line.Clone());
        }

        return document;
    }

    partial void OnSelectedDocumentTypeOptionChanged(StockDocumentTypeOption? value)
    {
        if (value is not null)
        {
            Type = value.Value;
        }
    }

    partial void OnSelectedLineChanged(StockDocumentLine? value)
    {
        IsLineEditorVisible = value is not null;
        EditLineCommand.NotifyCanExecuteChanged();
        DeleteLineCommand.NotifyCanExecuteChanged();
    }

    private bool HasSelectedLine() => SelectedLine is not null;

    [RelayCommand]
    private async Task AddMaterial()
    {
        var material = await _materialPickerService.PickMaterialAsync(OwnerWindow);
        if (material is null)
        {
            return;
        }

        var line = CreateLineFromMaterial(material);
        line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(DocumentTotalSum));
        Lines.Add(line);
        SelectedLine = line;
        IsLineEditorVisible = true;
        OnPropertyChanged(nameof(DocumentTotalSum));
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLine))]
    private void EditLine()
    {
        if (SelectedLine is null)
        {
            return;
        }

        IsLineEditorVisible = true;
    }

    [RelayCommand(CanExecute = nameof(HasSelectedLine))]
    private void DeleteLine()
    {
        if (SelectedLine is null)
        {
            return;
        }

        var index = Lines.IndexOf(SelectedLine);
        Lines.Remove(SelectedLine);
        SelectedLine = Lines.Count == 0
            ? null
            : Lines[Math.Clamp(index, 0, Lines.Count - 1)];
        OnPropertyChanged(nameof(DocumentTotalSum));
    }

    [RelayCommand]
    private void Save() => RequestClose?.Invoke(true);

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(false);

    private static StockDocumentLine CreateLineFromMaterial(MaterialDetail material)
    {
        var line = new StockDocumentLine
        {
            Article = material.Article,
            Name = material.Name,
            Unit = MaterialEnumLabels.MainUnit(material.MainUnit),
            Quantity = 1m,
            Price = material.BasePrice,
            Currency = MaterialEnumLabels.Currency(material.Currency)
        };

        line.PropertyChanged += (_, _) => { };
        return line;
    }
}
