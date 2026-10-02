using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.EditDialogs;
using NurSapat.App.Services.EditDialogs;
using NurSapat.Cad.UI.ViewModels;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Dialogs.Edit;

public sealed partial class CopyDrawingDialogViewModel : ObservableObject
{
    private readonly CadEditorViewModel _cadEditor;
    private readonly IDrawingExportService _drawingExportService;

    public ObservableCollection<int> ResolutionOptions { get; } = new([96, 144, 300, 600]);

    [ObservableProperty]
    private DrawingExportFormat _selectedFormat = DrawingExportFormat.Bmp;

    [ObservableProperty]
    private bool _includeDimensionLines = true;

    [ObservableProperty]
    private bool _includeAxes = true;

    [ObservableProperty]
    private bool _includeLabels = true;

    [ObservableProperty]
    private bool _includeDimensions = true;

    [ObservableProperty]
    private bool _includeSashes = true;

    [ObservableProperty]
    private bool _includeImposts = true;

    [ObservableProperty]
    private bool _includeGlazing = true;

    [ObservableProperty]
    private DrawingBackgroundMode _selectedBackground = DrawingBackgroundMode.White;

    [ObservableProperty]
    private int _scalePercent = 100;

    [ObservableProperty]
    private int _selectedResolution = 96;

    public string PreviewPlaceholderText => "Предпросмотр";

    public event EventHandler<bool>? CloseRequested;

    public CopyDrawingDialogViewModel(
        CadEditorViewModel cadEditor,
        IDrawingExportService drawingExportService,
        bool includeDimensionLines)
    {
        _cadEditor = cadEditor;
        _drawingExportService = drawingExportService;
        IncludeDimensionLines = includeDimensionLines;
        IncludeDimensions = includeDimensionLines;
    }

    partial void OnIncludeDimensionLinesChanged(bool value)
    {
        if (!value)
        {
            IncludeDimensions = false;
        }
        else
        {
            IncludeDimensions = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CanCopy))]
    private void Copy() => CloseRequested?.Invoke(this, true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    private bool CanCopy() => _cadEditor.FrameRoot is not null || _cadEditor.CanvasElements.Count > 0;

    public CopyDrawingParameters ToParameters() => new()
    {
        Format = SelectedFormat,
        IncludeDimensionLines = IncludeDimensionLines,
        IncludeAxes = IncludeAxes,
        IncludeLabels = IncludeLabels,
        IncludeDimensions = IncludeDimensions,
        IncludeSashes = IncludeSashes,
        IncludeImposts = IncludeImposts,
        IncludeGlazing = IncludeGlazing,
        Background = SelectedBackground,
        ScalePercent = ScalePercent,
        ResolutionDpi = SelectedResolution
    };

    public Task CopyAsync(CancellationToken cancellationToken = default) =>
        _drawingExportService.CopyToClipboardAsync(_cadEditor, ToParameters(), cancellationToken);
}
