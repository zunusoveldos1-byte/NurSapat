using CommunityToolkit.Mvvm.ComponentModel;

using CommunityToolkit.Mvvm.Input;

using NurSapat.Cad.Core.Geometry;

using NurSapat.Cad.Core.Models;

using NurSapat.Cad.UI.Models;

using NurSapat.Cad.UI.Rendering;
using NurSapat.Cad.UI.Services;
using NurSapat.Core.Models.Construction;

using NurSapat.Core.Models.Orders;

using System.Collections.ObjectModel;



namespace NurSapat.Cad.UI.ViewModels;



public sealed partial class CadEditorViewModel

{

    public bool IsSillToolActive => CurrentToolMode == WindowToolMode.Sill;



    public bool IsSillPropertiesPanelVisible =>

        WorkspaceMode == CadEditorWorkspaceMode.Editing

        && (FrameRoot?.HasSill == true

            || _constructionSelection.Any(item => item.Kind == CadSelectableKind.Sill));



    public ObservableCollection<ConstructionMaterialOption> SillMaterialOptions { get; } = [];



    private ConstructionMaterialOption? _selectedSillMaterialOption;



    public ConstructionMaterialOption? SelectedSillMaterialOption

    {

        get => _selectedSillMaterialOption;

        set

        {

            if (ReferenceEquals(_selectedSillMaterialOption, value))

            {

                return;

            }



            _selectedSillMaterialOption = value;

            OnPropertyChanged();

            ApplySillMaterialSelection(value);

        }

    }



    public bool IsSashMosquitoNetPanelVisible =>

        WorkspaceMode == CadEditorWorkspaceMode.Editing

        && _constructionSelection.Any(item => item.Kind == CadSelectableKind.Sash);



    public bool HasMosquitoNetOnSelectedSash

    {

        get => GetSelectedSash()?.HasMosquitoNet ?? false;

        set => SetMosquitoNetOnSelectedSash(value);

    }



    public bool CanAttachSillToFrame() =>

        FrameRoot?.GetConstructionRoot() is not null && HasRealFrameOnCanvas;



    [RelayCommand]

    private void ToggleMosquitoNetOnSelectedSash() =>

        SetMosquitoNetOnSelectedSash(!HasMosquitoNetOnSelectedSash);



    private GeometryElement? GetSelectedSash()

    {

        var sashItem = _constructionSelection.FirstOrDefault(item => item.Kind == CadSelectableKind.Sash);

        if (sashItem is null || sashItem.ElementId is not Guid sashId)

        {

            return null;

        }



        return CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);

    }



    private void SetMosquitoNetOnSelectedSash(bool enabled)

    {

        var sash = GetSelectedSash();

        if (sash is null)

        {

            return;

        }



        var leaf = SashGeometry.FindLeafForSash(this, sash);

        if (leaf is null)

        {

            return;

        }



        if (enabled)

        {

            if (FindMosquitoNetOnSash(sash) is not null)

            {

                sash.HasMosquitoNet = true;

                OnPropertyChanged(nameof(HasMosquitoNetOnSelectedSash));

                RequestSceneRefresh();

                return;

            }



            var net = CreateMosquitoNetElement(sash, leaf);

            if (net is null)

            {

                StatusText = "Не удалось разместить москитную сетку";

                return;

            }



            RecordPlacement("Москитная сетка", () =>

            {

                sash.HasMosquitoNet = true;

                AddCanvasElement(net);

                return true;

            });

        }

        else

        {

            RecordPlacement("Москитная сетка снята", () =>

            {

                sash.HasMosquitoNet = false;

                var net = FindMosquitoNetOnSash(sash);

                if (net is not null)

                {

                    CanvasElements.Remove(net);

                }



                return true;

            });

        }



        OnPropertyChanged(nameof(HasMosquitoNetOnSelectedSash));

        RequestSceneRefresh();

    }



    public async Task TryHandleSillClickAsync(CadPoint worldPoint)
    {
        if (CurrentToolMode != WindowToolMode.Sill || !CanAttachSillToFrame())
        {
            return;
        }

        if (!SillRenderer.TryHitBottomFrameProfile(this, worldPoint))
        {
            StatusText = "Кликните по нижнему профилю рамы снаружи";
            return;
        }

        var root = FrameRoot?.GetConstructionRoot();
        if (root is null)
        {
            return;
        }

        ReleaseModalDialogBlocking();

        var dialogResult = await _dialogService.ShowSillDialogAsync(root.Width, root.AttachedSill);
        if (dialogResult is null)
        {
            return;
        }

        if (!RecordPlacement("Подоконник установлен", () =>
            {
                AttachSillToFrame(dialogResult);
                SyncSillCanvasElement();
                RefreshSillMaterialOptions();
                OnPropertyChanged(nameof(IsSillPropertiesPanelVisible));
                return true;
            }))
        {
            return;
        }

        RequestSceneRefresh();
        StatusText = "Подоконник установлен";
    }

    private void AttachSillToFrame(SillDialogResult? dialog = null)
    {
        var root = FrameRoot?.GetConstructionRoot();
        if (root is null)
        {
            return;
        }

        root.AttachedSill ??= new SillElement();
        var sill = root.AttachedSill;

        if (dialog is null)
        {
            sill.ThicknessMm = SillElement.StandardThicknessMm;
            sill.DepthMm = 200;
            sill.LeftOverhangMm = SillElement.DefaultSideOverhangMm;
            sill.RightOverhangMm = SillElement.DefaultSideOverhangMm;
            sill.MaterialId ??= SillElement.DefaultMaterialId;
            sill.Color = SillRenderer.ResolveRenderColor(sill);
            sill.WidthMm = sill.ResolveWidthForFrame(root.Width);
            return;
        }

        sill.ThicknessMm = dialog.ThicknessMm;
        sill.WidthMm = dialog.WidthMm;
        sill.LeftOverhangMm = dialog.LeftOverhangMm;
        sill.RightOverhangMm = dialog.RightOverhangMm;
        sill.MaterialId = dialog.MaterialId;
        sill.Color = dialog.Color;
        sill.DepthMm = 200;
    }



    internal void SyncSillCanvasElement()

    {

        var root = FrameRoot?.GetConstructionRoot();

        if (root?.AttachedSill is not { } sill)

        {

            for (var i = CanvasElements.Count - 1; i >= 0; i--)

            {

                if (CanvasElements[i].Kind == GeometryElementKind.Sill)

                {

                    CanvasElements.RemoveAt(i);

                }

            }



            RefreshConstructionVolume();

            OnPropertyChanged(nameof(IsSillPropertiesPanelVisible));

            return;

        }



        var element = CanvasElements.FirstOrDefault(e => e.Kind == GeometryElementKind.Sill);

        if (element is null)

        {

            element = new GeometryElement

            {

                Kind = GeometryElementKind.Sill,

                Label = "Подоконник",

                SourceToolId = "sill"

            };

            AddCanvasElement(element);

        }



        var thickness = sill.ThicknessMm > 0 ? sill.ThicknessMm : SillElement.StandardThicknessMm;

        var leftOverhang = sill.LeftOverhangMm > 0 ? sill.LeftOverhangMm : SillElement.DefaultSideOverhangMm;

        var width = sill.WidthMm > 0 ? sill.WidthMm : sill.ResolveWidthForFrame(root.Width);

        var left = root.X - leftOverhang;

        var centerX = left + width / 2.0;

        var bottomY = root.Y - thickness / 2.0;



        element.X = (decimal)centerX;

        element.Y = (decimal)bottomY;

        element.WidthMm = (decimal)width;

        element.HeightMm = (decimal)thickness;

        element.EndX = (decimal)(centerX + width / 2.0);

        element.EndY = (decimal)bottomY;

        element.MaterialId = sill.MaterialId;

        RefreshConstructionVolume();

        OnPropertyChanged(nameof(IsSillPropertiesPanelVisible));

    }



    private void SyncSillToFrameWidth()

    {

        var root = FrameRoot?.GetConstructionRoot();

        if (root?.AttachedSill is not { } sill)

        {

            return;

        }



        sill.WidthMm = sill.ResolveWidthForFrame(root.Width);

        SyncSillCanvasElement();

    }



    private void RefreshSillMaterialOptions()

    {

        var previousId = _selectedSillMaterialOption?.MaterialId

            ?? FrameRoot?.GetConstructionRoot()?.AttachedSill?.MaterialId;



        SillMaterialOptions.Clear();

        foreach (var material in ConstructionMaterialCatalog.ForKind(GeometryElementKind.Sill)

                     .OrderBy(item => item.Name))

        {

            SillMaterialOptions.Add(ConstructionMaterialOption.FromMaterial(material));

        }



        var next = previousId is Guid id

            ? SillMaterialOptions.FirstOrDefault(option => option.MaterialId == id)

            : SillMaterialOptions.FirstOrDefault(option => option.MaterialId == SillElement.DefaultMaterialId);



        _selectedSillMaterialOption = next ?? SillMaterialOptions.FirstOrDefault();

        OnPropertyChanged(nameof(SelectedSillMaterialOption));

    }



    private void ApplySillMaterialSelection(ConstructionMaterialOption? option)

    {

        var root = FrameRoot?.GetConstructionRoot();

        if (root?.AttachedSill is not { } sill)

        {

            return;

        }



        sill.MaterialId = option?.MaterialId ?? SillElement.DefaultMaterialId;

        sill.Color = SillRenderer.ResolveRenderColorForMaterial(sill.MaterialId);

        SyncSillCanvasElement();

        RequestSceneRefresh();

    }



    private void SyncSillPanelFromSelection()

    {

        RefreshSillMaterialOptions();

        OnPropertyChanged(nameof(IsSillPropertiesPanelVisible));

    }



    private bool TryPlaceCorrugationInLeaf(FrameNode leaf, GlazingType fillType)
    {
        if (!CanApplyCorrugationInLeaf(leaf))
        {
            StatusText = FindCorrugationInLeaf(leaf) is not null
                ? "Гофра уже установлена в этом проёме"
                : "Проём слишком мал для гофры";
            return false;
        }

        var element = CreateCorrugationElement(leaf, fillType);
        if (element is null)
        {
            StatusText = "Проём слишком мал для гофры";
            return false;
        }

        var label = ResolveCorrugationLabel(fillType);

        if (!RecordPlacement(label, () =>
            {
                AddCanvasElement(element);
                return true;
            }))
        {
            return false;
        }

        StatusText = $"{label} установлена";
        RequestSceneRefresh();
        return true;
    }

    private bool CanApplyCorrugationInLeaf(FrameNode leaf) =>
        FindCorrugationInLeaf(leaf) is null;

    private GeometryElement? FindCorrugationInLeaf(FrameNode leaf) =>
        CanvasElements.FirstOrDefault(
            e => e.Kind == GeometryElementKind.Corrugation && ContainsLeaf(e, leaf));

    private GeometryElement? CreateCorrugationElement(FrameNode leaf, GlazingType fillType)
    {
        if (FrameRoot is null)
        {
            return null;
        }

        var sash = FindSashInLeaf(leaf);
        if (sash is not null)
        {
            var inner = SashGeometry.TryResolveInnerBounds(this, sash, out var liveInner)
                ? liveInner
                : SashGeometry.ResolveLeafInnerOpeningBounds(this, leaf);
            var outer = SashGeometry.ExpandInnerBoundsToOuter(inner, this);
            SashGeometry.TryResolveGlassOpeningBounds(
                this,
                inner,
                outer,
                SashGeometry.SashDrawProfileThicknessMm,
                out var glass);
            if (glass.Width < 20 || glass.Height < 20)
            {
                return null;
            }

            return new GeometryElement
            {
                Kind = GeometryElementKind.Corrugation,
                Label = ResolveCorrugationLabel(fillType),
                X = (decimal)((glass.Left + glass.Right) / 2.0),
                Y = (decimal)((glass.Bottom + glass.Top) / 2.0),
                WidthMm = (decimal)glass.Width,
                HeightMm = (decimal)glass.Height,
                FillType = fillType,
                SourceToolId = ResolveCorrugationSourceToolId(fillType),
                HostSashId = sash.Id
            };
        }

        if (!SashGeometry.TryResolveFixedFrameGlassOpeningBounds(this, leaf, out var fixedGlass))
        {
            var (innerX, innerY, innerW, innerH) = SashGeometry.ResolveLeafInnerOpening(this, leaf);
            if (innerW < 40 || innerH < 40)
            {
                return null;
            }

            return new GeometryElement
            {
                Kind = GeometryElementKind.Corrugation,
                Label = ResolveCorrugationLabel(fillType),
                X = (decimal)(innerX + innerW / 2.0),
                Y = (decimal)(innerY + innerH / 2.0),
                WidthMm = (decimal)innerW,
                HeightMm = (decimal)innerH,
                FillType = fillType,
                SourceToolId = ResolveCorrugationSourceToolId(fillType)
            };
        }

        if (fixedGlass.Width < 20 || fixedGlass.Height < 20)
        {
            return null;
        }

        return new GeometryElement
        {
            Kind = GeometryElementKind.Corrugation,
            Label = ResolveCorrugationLabel(fillType),
            X = (decimal)((fixedGlass.Left + fixedGlass.Right) / 2.0),
            Y = (decimal)((fixedGlass.Bottom + fixedGlass.Top) / 2.0),
            WidthMm = (decimal)fixedGlass.Width,
            HeightMm = (decimal)fixedGlass.Height,
            FillType = fillType,
            SourceToolId = ResolveCorrugationSourceToolId(fillType)
        };
    }

    private static string ResolveCorrugationLabel(GlazingType fillType) => fillType switch
    {
        GlazingType.HorizontalCorrugation => "Гофра горизонтальная",
        GlazingType.SlantedCorrugation => "Гофра наклонная",
        _ => "Гофра вертикальная"
    };

    private static string ResolveCorrugationSourceToolId(GlazingType fillType) => fillType switch
    {
        GlazingType.HorizontalCorrugation => "corrugation-h",
        GlazingType.SlantedCorrugation => "corrugation-slanted",
        _ => "corrugation-v"
    };

    private void SyncCorrugationElementToLayout(GeometryElement corrugation)
    {
        if (corrugation.HostSashId is Guid sashId)
        {
            var sash = CanvasElements.FirstOrDefault(e => e.Id == sashId && e.Kind == GeometryElementKind.Sash);
            var leaf = sash is not null ? SashGeometry.FindLeafForSash(this, sash) : null;
            if (sash is null || leaf is null || corrugation.FillType is not { } fillType)
            {
                return;
            }

            var updated = CreateCorrugationElement(leaf, fillType);
            if (updated is null)
            {
                return;
            }

            corrugation.X = updated.X;
            corrugation.Y = updated.Y;
            corrugation.WidthMm = updated.WidthMm;
            corrugation.HeightMm = updated.HeightMm;
            return;
        }

        var hostLeaf = FrameRoot?.FindLeafAt((double)corrugation.X, (double)corrugation.Y);
        if (hostLeaf is null || corrugation.FillType is not { } type)
        {
            return;
        }

        var refreshed = CreateCorrugationElement(hostLeaf, type);
        if (refreshed is null)
        {
            return;
        }

        corrugation.X = refreshed.X;
        corrugation.Y = refreshed.Y;
        corrugation.WidthMm = refreshed.WidthMm;
        corrugation.HeightMm = refreshed.HeightMm;
    }

    private static ConstructionShape ResolveShapeForMode(WindowToolMode mode) =>
        mode switch
        {
            WindowToolMode.FakeFrameCircle => ConstructionShape.Circle,
            WindowToolMode.FakeFrameTriangle => ConstructionShape.Triangle,
            _ => ConstructionShape.Rectangle
        };
}
