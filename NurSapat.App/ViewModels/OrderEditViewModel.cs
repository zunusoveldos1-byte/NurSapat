using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.UI.ViewModels;

namespace NurSapat.App.ViewModels;

public sealed partial class OrderEditViewModel : ObservableObject
{
    public OrderCalculationState Calculation { get; }

    public OrderEditViewModel(OrderCalculationState calculation)
    {
        Calculation = calculation;
    }
}
