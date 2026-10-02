using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.Services;

public sealed record DecorativeSpokeDialogResult(
    DecorativeSpokeType SpokeType,
    double Thickness,
    string Color,
    int RowsCount,
    int ColumnsCount,
    int RayCount);
