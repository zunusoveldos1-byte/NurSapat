using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Project;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels;

public sealed partial class PrintDialogViewModel : ObservableObject
{
    private readonly CadEditorViewModel _cadEditor;

    public ObservableCollection<string> Printers { get; } = new();

    [ObservableProperty]
    private string? _selectedPrinter;

    [ObservableProperty]
    private int _copies = 1;

    [ObservableProperty]
    private PaperFormat _paperFormat = PaperFormat.A4;

    [ObservableProperty]
    private PageOrientation _orientation = PageOrientation.Landscape;

    [ObservableProperty]
    private PrintColorMode _colorMode = PrintColorMode.Color;

    [ObservableProperty]
    private double _scalePercent = 100;

    [ObservableProperty]
    private bool _printDimensions = true;

    [ObservableProperty]
    private bool _printCoordinates;

    public string PreviewTitle => _cadEditor.Document.Name;

    public string PreviewSizeText
    {
        get
        {
            var width = _cadEditor.SelectedElementLength;
            var height = _cadEditor.SelectedElementHeight;
            return width > 0 && height > 0
                ? $"{width:0} × {height:0} мм"
                : "Нет данных для предпросмотра";
        }
    }

    public event EventHandler<bool>? CloseRequested;

    public PrintDialogViewModel(PrintSettings settings, CadEditorViewModel cadEditor)
    {
        _cadEditor = cadEditor;
        ScalePercent = settings.ScalePercent;
        PaperFormat = settings.PaperFormat;
        Orientation = settings.Orientation;
        ColorMode = settings.ColorMode;
        PrintDimensions = settings.PrintDimensions;
        PrintCoordinates = settings.PrintCoordinates;
        LoadPrinters();
    }

    [RelayCommand]
    private void Print() => CloseRequested?.Invoke(this, true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    public PrintSettings ToSettings() => new()
    {
        ScalePercent = ScalePercent,
        PaperFormat = PaperFormat,
        Orientation = Orientation,
        ColorMode = ColorMode,
        PrintDimensions = PrintDimensions,
        PrintCoordinates = PrintCoordinates
    };

    public Task PrintAsync(CancellationToken cancellationToken = default)
    {
        // Печать делегируется системному API; на данном этапе фиксируем намерение пользователя.
        return Task.CompletedTask;
    }

    private void LoadPrinters()
    {
        Printers.Clear();
        Printers.Add("Принтер по умолчанию");

        try
        {
            if (OperatingSystem.IsWindows())
            {
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    Printers.Add(printer);
                }
            }
        }
        catch
        {
            // Если список принтеров недоступен, остаётся пункт по умолчанию.
        }

        SelectedPrinter = Printers.FirstOrDefault();
    }
}
