using System.Text.Json;
using System.Text.Json.Serialization;
using NurSapat.App.Services.Documents;
using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models.Orders;
using NurSapat.Core.Models.Project;

namespace NurSapat.App.Services;

internal static class ConstructionDrawingStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public static string Serialize(CadEditorViewModel.CadEditorSnapshot snapshot)
    {
        var data = CadProjectSnapshotHelper.Capture(snapshot);
        return JsonSerializer.Serialize(data, Options);
    }

    public static CadEditorViewModel.CadEditorSnapshot? TryDeserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            return null;
        }

        try
        {
            var data = JsonSerializer.Deserialize<CadProjectData>(json, Options);
            return data is null ? null : CadProjectSnapshotHelper.ToSnapshot(data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void ApplyPreview(CadEditorViewModel editor, OrderConstruction construction)
    {
        editor.IsViewOnlyMode = true;
        editor.IsGridEnabled = false;
        editor.ShowDimensions = false;

        var elements = construction.Geometry.Elements.ToList();
        var snapshot = TryDeserialize(construction.Geometry.DrawingJson);
        if (snapshot is not null)
        {
            editor.ApplySnapshot(snapshot, elements);
        }
        else if (elements.Count > 0)
        {
            editor.SetDesignElements(elements);
            editor.FrameRoot = TryCreateFrameRootFromElements(elements);
            if (editor.FrameRoot is not null && editor.CanvasElements.Count > 0)
            {
                editor.FitThumbnailPreview(boundsPaddingRatio: 0.36);
            }
        }
        else
        {
            editor.PrepareForNewConstruction();
        }

        if (editor.FrameRoot is not null || editor.CanvasElements.Count > 0)
        {
            editor.FitThumbnailPreview(boundsPaddingRatio: 0.36);
        }

        editor.RequestSceneRefresh();
    }

    private static NurSapat.Cad.Core.Models.FrameNode? TryCreateFrameRootFromElements(
        IReadOnlyList<GeometryElement> elements)
    {
        var frameElement = elements.FirstOrDefault(e =>
            e.Kind is GeometryElementKind.Frame
                or GeometryElementKind.FictitiousFrame
                or GeometryElementKind.DummyFrame);
        if (frameElement is null)
        {
            return null;
        }

        var width = (double)frameElement.WidthMm;
        var height = (double)frameElement.HeightMm;
        return NurSapat.Cad.Core.Models.FrameNode.CreateRoot(
            (double)frameElement.X - width / 2.0,
            (double)frameElement.Y - height / 2.0,
            width,
            height);
    }
}
