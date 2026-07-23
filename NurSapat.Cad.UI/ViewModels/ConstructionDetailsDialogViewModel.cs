using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Calculations;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class ConstructionDetailsDialogViewModel : ObservableObject
{
    public event EventHandler<bool>? CloseRequested;

    public ObservableCollection<ConstructionDetailsRow> Rows { get; } = [];

    public string TotalVolumeText { get; }

    public string BillableCountText { get; }

    public ConstructionDetailsDialogViewModel(ConstructionVolumeSummary summary, IEnumerable<GeometryElement> elements)
    {
        var byId = elements.ToDictionary(e => e.Id);
        foreach (var line in summary.Lines)
        {
            byId.TryGetValue(line.ElementId, out var element);
            Rows.Add(new ConstructionDetailsRow(
                line.Kind.ToString(),
                string.IsNullOrWhiteSpace(line.Label) ? line.Kind.ToString() : line.Label,
                element is null ? "—" : $"{element.WidthMm:0}×{element.HeightMm:0}",
                line.MaterialName ?? "—",
                line.SeriesName ?? "—",
                $"{line.VolumeMm3 / 1_000_000.0:0.###} л",
                line.IsBillable ? "Да" : "Нет"));
        }

        TotalVolumeText = $"Итого объём: {summary.TotalVolumeLiters:0.###} л";
        BillableCountText = $"Учитываемых элементов: {summary.Lines.Count(l => l.IsBillable)} из {summary.Lines.Count}";
    }

    [RelayCommand]
    private void Close() => CloseRequested?.Invoke(this, true);
}

public sealed record ConstructionDetailsRow(
    string Kind,
    string Label,
    string Size,
    string Material,
    string Series,
    string Volume,
    string Billable);
