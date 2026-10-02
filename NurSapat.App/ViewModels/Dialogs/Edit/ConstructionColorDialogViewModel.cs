using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.EditDialogs;
using NurSapat.App.Services;
using NurSapat.App.Services.EditDialogs;
using NurSapat.App.ViewModels;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels.Dialogs.Edit;

public sealed partial class ConstructionColorDialogViewModel : ObservableObject
{
    private readonly OrderWorkspaceViewModel _workspace;
    private readonly CadEditorViewModel _cadEditor;
    private readonly IConstructionColorService _constructionColorService;

    public CadEditorViewModel PreviewEditor { get; }

    public ObservableCollection<StandardColorOption> StandardColors { get; } =
        new(StandardColorCatalog.Colors);

    public ObservableCollection<string> RecentColorHexes { get; } = new();

    [ObservableProperty]
    private Color _selectedColor = Color.Parse("#FFFFFF");

    [ObservableProperty]
    private string _selectedColorName = "Белый";

    [ObservableProperty]
    private bool _changeProfile = true;

    [ObservableProperty]
    private bool _changeImposts = true;

    [ObservableProperty]
    private bool _changeSashes = true;

    [ObservableProperty]
    private bool _changeLayout = true;

    [ObservableProperty]
    private bool _changeDecorativeElements = true;

    [ObservableProperty]
    private StandardColorOption? _selectedStandardColor;

    public event EventHandler<bool>? CloseRequested;

    public ConstructionColorDialogViewModel(
        OrderWorkspaceViewModel workspace,
        CadEditorViewModel cadEditor,
        IConstructionColorService constructionColorService,
        ICadPreviewEditorFactory previewEditorFactory,
        IRecentColorsService recentColorsService,
        OrderConstruction construction)
    {
        _workspace = workspace;
        _cadEditor = cadEditor;
        _constructionColorService = constructionColorService;

        PreviewEditor = previewEditorFactory.CreatePreviewEditor();
        ConstructionDrawingStore.ApplyPreview(PreviewEditor, construction);

        var initial = constructionColorService.CreateFromConstruction(construction);
        SelectedColor = ParseColor(initial.ColorHex);
        SelectedColorName = initial.ColorName;
        ChangeProfile = initial.Targets.HasFlag(ConstructionColorTargets.Profile);
        ChangeImposts = initial.Targets.HasFlag(ConstructionColorTargets.Imposts);
        ChangeSashes = initial.Targets.HasFlag(ConstructionColorTargets.Sashes);
        ChangeLayout = initial.Targets.HasFlag(ConstructionColorTargets.Layout);
        ChangeDecorativeElements = initial.Targets.HasFlag(ConstructionColorTargets.DecorativeElements);

        foreach (var hex in recentColorsService.GetRecentColors())
        {
            RecentColorHexes.Add(hex);
        }

        SelectedStandardColor = StandardColors.FirstOrDefault(c =>
            !c.IsCustomPicker && string.Equals(c.Hex, initial.ColorHex, StringComparison.OrdinalIgnoreCase))
            ?? StandardColors.First(c => c.Name == "Белый");
    }

    partial void OnSelectedColorChanged(Color value)
    {
        SelectedColorName = StandardColorCatalog.FindNameByHex(value.ToString()) ?? "Пользовательский";
        ApplyCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SelectStandardColor(StandardColorOption? option)
    {
        if (option is null)
        {
            return;
        }

        SelectedStandardColor = option;
        if (option.IsCustomPicker)
        {
            return;
        }

        SelectedColor = ParseColor(option.Hex);
        SelectedColorName = option.Name;
    }

    [RelayCommand]
    private void SelectRecentColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return;
        }

        SelectedColor = ParseColor(hex);
        SelectedColorName = StandardColorCatalog.FindNameByHex(hex) ?? "Пользовательский";
    }

    [RelayCommand(CanExecute = nameof(CanApply))]
    private void Apply() => CloseRequested?.Invoke(this, true);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    private bool CanApply() => true;

    public void Commit()
    {
        _constructionColorService.ApplyToConstruction(_workspace, _cadEditor, ToParameters());
    }

    public ConstructionColorParameters ToParameters()
    {
        var targets = ConstructionColorTargets.None;
        if (ChangeProfile) targets |= ConstructionColorTargets.Profile;
        if (ChangeImposts) targets |= ConstructionColorTargets.Imposts;
        if (ChangeSashes) targets |= ConstructionColorTargets.Sashes;
        if (ChangeLayout) targets |= ConstructionColorTargets.Layout;
        if (ChangeDecorativeElements) targets |= ConstructionColorTargets.DecorativeElements;

        return new ConstructionColorParameters
        {
            ColorName = SelectedColorName,
            ColorHex = StandardColorCatalog.NormalizeHex(SelectedColor.ToString()),
            Targets = targets == ConstructionColorTargets.None ? ConstructionColorTargets.All : targets
        };
    }

    private static Color ParseColor(string hex) =>
        Color.TryParse(hex, out var color) ? color : Colors.White;
}
