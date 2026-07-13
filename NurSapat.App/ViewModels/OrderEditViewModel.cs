using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.ViewModels;

namespace NurSapat.App.ViewModels;

public sealed partial class OrderEditViewModel : ObservableObject
{
    public OrderSheetViewModel Sheet { get; }

    public OrderEditViewModel(OrderSheetViewModel sheet) => Sheet = sheet;
}
