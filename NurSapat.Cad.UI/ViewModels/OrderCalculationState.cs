using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Core.Parametric;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class OrderCalculationState : ObservableObject
{
    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    [ObservableProperty]
    private double _itemArea;

    [ObservableProperty]
    private double _glassArea;

    [ObservableProperty]
    private double _profileLength;

    [ObservableProperty]
    private decimal _estimatedMaterialCost;

    public void UpdateFrom(WindowStructure structure)
    {
        Width = structure.Options.Width;
        Height = structure.Options.Height;
        ItemArea = Math.Round(Width * Height / 1_000_000d, 3);
        GlassArea = Math.Round(structure.LeafSections
            .Where(s => s.Fill.Kind is SectionFillKind.Glazing or SectionFillKind.Sash)
            .Select(s => s.GetGlassRect(structure.Options))
            .Sum(r => r.Width * r.Height) / 1_000_000d, 3);
        ProfileLength = Math.Round((Width + Height) * 2 / 1000d, 2);
        EstimatedMaterialCost = (decimal)Math.Round(ItemArea * 4200 + GlassArea * 1800 + ProfileLength * 650, 2);
    }
}
