using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.EditDialogs;
using NurSapat.App.Services;
using NurSapat.App.Services.EditDialogs;
using NurSapat.App.ViewModels;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.App.ViewModels.Dialogs.Edit;

public sealed partial class OrderColorDialogViewModel : ObservableObject
{
    private readonly OrderWorkspaceViewModel _workspace;
    private readonly CadEditorViewModel _cadEditor;
    private readonly IOrderColorService _orderColorService;

    public CadEditorViewModel PreviewEditor { get; }

    public int ConstructionCount => _workspace.Order.Constructions.Count;

    [ObservableProperty]
    private Color _selectedColor = Color.Parse("#FFFFFF");

    [ObservableProperty]
    private string _selectedColorName = "Белый";

    [ObservableProperty]
    private bool _applyToAllConstructions = true;

    [ObservableProperty]
    private bool _applyToAllSashes = true;

    [ObservableProperty]
    private bool _applyToAllImposts = true;

    [ObservableProperty]
    private bool _applyToAllDecorativeElements = true;

    public event EventHandler<bool>? CloseRequested;

    public OrderColorDialogViewModel(
        OrderWorkspaceViewModel workspace,
        CadEditorViewModel cadEditor,
        IOrderColorService orderColorService,
        ICadPreviewEditorFactory previewEditorFactory)
    {
        _workspace = workspace;
        _cadEditor = cadEditor;
        _orderColorService = orderColorService;

        PreviewEditor = previewEditorFactory.CreatePreviewEditor();
        var previewConstruction = workspace.Order.SelectedConstruction
            ?? workspace.Order.EnsureDefaultConstruction();
        ConstructionDrawingStore.ApplyPreview(PreviewEditor, previewConstruction);

        var appearance = previewConstruction.Appearance;
        SelectedColor = ParseColor(appearance.ColorHex);
        SelectedColorName = appearance.ColorName;
    }

    partial void OnSelectedColorChanged(Color value)
    {
        SelectedColorName = StandardColorCatalog.FindNameByHex(value.ToString()) ?? "Пользовательский";
        ApplyCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply() => CloseRequested?.Invoke(this, true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    private bool CanApply() => ConstructionCount > 0;

    public void Commit()
    {
        _orderColorService.ApplyToOrder(_workspace, _cadEditor, ToParameters());
    }

    public OrderColorParameters ToParameters() => new()
    {
        ColorName = SelectedColorName,
        ColorHex = StandardColorCatalog.NormalizeHex(SelectedColor.ToString()),
        ApplyToAllConstructions = ApplyToAllConstructions,
        ApplyToAllSashes = ApplyToAllSashes,
        ApplyToAllImposts = ApplyToAllImposts,
        ApplyToAllDecorativeElements = ApplyToAllDecorativeElements
    };

    private static Color ParseColor(string hex) =>
        Color.TryParse(hex, out var color) ? color : Colors.White;
}
