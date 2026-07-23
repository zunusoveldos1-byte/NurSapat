using NurSapat.App.Models.EditDialogs;
using NurSapat.App.ViewModels;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.App.Services.EditDialogs;

public interface IOrderColorService
{
    void ApplyToOrder(
        OrderWorkspaceViewModel workspace,
        CadEditorViewModel cadEditor,
        OrderColorParameters parameters);
}

public sealed class OrderColorService : IOrderColorService
{
    private readonly IRecentColorsService _recentColorsService;

    public OrderColorService(IRecentColorsService recentColorsService)
    {
        _recentColorsService = recentColorsService;
    }

    public void ApplyToOrder(
        OrderWorkspaceViewModel workspace,
        CadEditorViewModel cadEditor,
        OrderColorParameters parameters)
    {
        var colorHex = StandardColorCatalog.NormalizeHex(parameters.ColorHex);
        var colorName = parameters.ColorName;

        foreach (var construction in workspace.Order.Constructions)
        {
            if (!parameters.ApplyToAllConstructions)
            {
                continue;
            }

            construction.Appearance.ColorName = colorName;
            construction.Appearance.ColorHex = colorHex;
            construction.Appearance.ChangeProfile = true;
            construction.Appearance.ChangeImposts = parameters.ApplyToAllImposts;
            construction.Appearance.ChangeSashes = parameters.ApplyToAllSashes;
            construction.Appearance.ChangeDecorativeElements = parameters.ApplyToAllDecorativeElements;
            construction.Appearance.ChangeLayout = true;
        }

        var active = workspace.Order.SelectedConstruction;
        if (active is not null)
        {
            cadEditor.SelectedMaterialColor = colorName;
        }

        workspace.RefreshConstructionListDisplay(active?.Id);
        _recentColorsService.Remember(colorHex);
        workspace.NotifyModified();
        cadEditor.RequestSceneRefresh();
    }
}
