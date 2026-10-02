namespace NurSapat.Cad.UI.Services;

public sealed record SillDialogResult(
    double ThicknessMm,
    double WidthMm,
    double LeftOverhangMm,
    double RightOverhangMm,
    Guid MaterialId,
    string Color);
