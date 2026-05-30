using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Core.Settings;

namespace NurSapat.App.ViewModels;

public sealed partial class ProductionOptionsViewModel : ObservableObject
{
    [ObservableProperty]
    private decimal _weldingAllowance;

    [ObservableProperty]
    private decimal _glassGlazingGap;

    [ObservableProperty]
    private decimal _impostMillingAllowance;

    [ObservableProperty]
    private decimal _sashOverlap;

    public ProductionOptionsViewModel(ProductionOptions options)
    {
        WeldingAllowance = options.WeldingAllowance;
        GlassGlazingGap = options.GlassGlazingGap;
        ImpostMillingAllowance = options.ImpostMillingAllowance;
        SashOverlap = options.SashOverlap;
    }
}
