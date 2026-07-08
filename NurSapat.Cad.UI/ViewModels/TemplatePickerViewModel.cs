using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.UI.Models;
using NurSapat.Cad.UI.Services;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class TemplatePickerViewModel : ObservableObject
{
    private readonly CadTemplateStore _store;
    private readonly CadEditorViewModel _editor;

    public event EventHandler<bool>? CloseRequested;

    public IReadOnlyList<CadDrawingTemplate> Templates => _store.Templates;

    [ObservableProperty]
    private CadDrawingTemplate? _selectedTemplate;

    public TemplatePickerViewModel(CadTemplateStore store, CadEditorViewModel editor)
    {
        _store = store;
        _editor = editor;
        SelectedTemplate = Templates.FirstOrDefault();
    }

    public CadDrawingTemplate? AppliedTemplate { get; private set; }

    [RelayCommand]
    private void AddTemplate()
    {
        var frame = _editor.CanvasElements.FirstOrDefault();
        var nextIndex = _store.Templates.Count + 1;
        var template = new CadDrawingTemplate
        {
            Name = $"Шаблон {nextIndex}",
            WidthMm = _editor.Length > 0 ? _editor.Length : 1000,
            HeightMm = _editor.Height > 0 ? _editor.Height : 1000,
            IsFakeFrame = frame?.Kind is GeometryElementKind.DummyFrame or GeometryElementKind.FictitiousFrame
        };
        _store.Templates.Add(template);
        SelectedTemplate = template;
    }

    [RelayCommand]
    private void DeleteTemplate()
    {
        if (SelectedTemplate is null || _store.Templates.Count <= 1)
        {
            return;
        }

        var removed = SelectedTemplate;
        _store.Templates.Remove(removed);
        SelectedTemplate = _store.Templates.FirstOrDefault();
    }

    [RelayCommand]
    private void Apply()
    {
        AppliedTemplate = SelectedTemplate;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
