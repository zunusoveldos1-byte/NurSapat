using NurSapat.Cad.UI.ViewModels;
using NurSapat.Core.Models;

namespace NurSapat.App.ViewModels;

/// <summary>
/// Общий контекст заказа: связывает чертёж, лист заказа и пересчёт.
/// </summary>
public sealed class OrderWorkspaceViewModel
{
    public Order Order { get; } = new();

    public OrderSheetViewModel Sheet { get; }

    public OrderDrawingViewModel Drawing { get; }

    public OrderWorkspaceViewModel(CadEditorViewModel cadEditor)
    {
        Sheet = new OrderSheetViewModel(Order);
        Drawing = new OrderDrawingViewModel(Order, Sheet, cadEditor);
    }

    public void RecalculateAll() => Sheet.RecalculateAll();
}
