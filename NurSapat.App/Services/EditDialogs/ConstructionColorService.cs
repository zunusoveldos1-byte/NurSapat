using NurSapat.App.Models.EditDialogs;
using NurSapat.App.ViewModels;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;

namespace NurSapat.App.Services.EditDialogs;

public interface IConstructionColorService
{
    void ApplyToConstruction(
        OrderWorkspaceViewModel workspace,
        CadEditorViewModel cadEditor,
        ConstructionColorParameters parameters);

    ConstructionColorParameters CreateFromConstruction(OrderConstruction construction);
}

public sealed class ConstructionColorService : IConstructionColorService
{
    private readonly IRecentColorsService _recentColorsService;

    public ConstructionColorService(IRecentColorsService recentColorsService)
    {
        _recentColorsService = recentColorsService;
    }

    public void ApplyToConstruction(
        OrderWorkspaceViewModel workspace,
        CadEditorViewModel cadEditor,
        ConstructionColorParameters parameters)
    {
        var construction = workspace.Order.SelectedConstruction ?? workspace.Order.EnsureDefaultConstruction();
        var appearance = construction.Appearance;

        appearance.ColorName = parameters.ColorName;
        appearance.ColorHex = StandardColorCatalog.NormalizeHex(parameters.ColorHex);
        appearance.ChangeProfile = parameters.Targets.HasFlag(ConstructionColorTargets.Profile);
        appearance.ChangeImposts = parameters.Targets.HasFlag(ConstructionColorTargets.Imposts);
        appearance.ChangeSashes = parameters.Targets.HasFlag(ConstructionColorTargets.Sashes);
        appearance.ChangeLayout = parameters.Targets.HasFlag(ConstructionColorTargets.Layout);
        appearance.ChangeDecorativeElements = parameters.Targets.HasFlag(ConstructionColorTargets.DecorativeElements);

        if (workspace.Order.SelectedConstruction?.Id == construction.Id)
        {
            cadEditor.SelectedMaterialColor = appearance.ColorName;
        }

        _recentColorsService.Remember(appearance.ColorHex);
        workspace.NotifyModified();
        cadEditor.RequestSceneRefresh();
    }

    public ConstructionColorParameters CreateFromConstruction(OrderConstruction construction)
    {
        var appearance = construction.Appearance;
        var targets = ConstructionColorTargets.None;
        if (appearance.ChangeProfile) targets |= ConstructionColorTargets.Profile;
        if (appearance.ChangeImposts) targets |= ConstructionColorTargets.Imposts;
        if (appearance.ChangeSashes) targets |= ConstructionColorTargets.Sashes;
        if (appearance.ChangeLayout) targets |= ConstructionColorTargets.Layout;
        if (appearance.ChangeDecorativeElements) targets |= ConstructionColorTargets.DecorativeElements;

        if (targets == ConstructionColorTargets.None)
        {
            targets = ConstructionColorTargets.All;
        }

        return new ConstructionColorParameters
        {
            ColorName = appearance.ColorName,
            ColorHex = appearance.ColorHex,
            Targets = targets
        };
    }
}
