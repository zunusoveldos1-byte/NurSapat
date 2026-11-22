using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Models.Construction;
using NurSapat.Core.Models.Orders;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class SillDialogViewModel : ObservableObject
{
    public event EventHandler<bool>? CloseRequested;

    public IReadOnlyList<SillMaterialOption> MaterialOptions { get; }

    [ObservableProperty]
    private string _thicknessText = "30";

    [ObservableProperty]
    private string _widthText = "1280";

    [ObservableProperty]
    private string _leftOverhangText = "40";

    [ObservableProperty]
    private string _rightOverhangText = "40";

    [ObservableProperty]
    private SillMaterialOption? _selectedMaterial;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public double ResultThicknessMm { get; private set; } = SillElement.StandardThicknessMm;

    public double ResultWidthMm { get; private set; }

    public double ResultLeftOverhangMm { get; private set; } = SillElement.DefaultSideOverhangMm;

    public double ResultRightOverhangMm { get; private set; } = SillElement.DefaultSideOverhangMm;

    public Guid ResultMaterialId { get; private set; } = SillElement.DefaultMaterialId;

    public string ResultColor { get; private set; } = SillElement.DefaultColorHex;

    public SillDialogViewModel(double frameWidthMm, SillElement? existing = null)
    {
        var left = existing?.LeftOverhangMm ?? SillElement.DefaultSideOverhangMm;
        var right = existing?.RightOverhangMm ?? SillElement.DefaultSideOverhangMm;
        var width = existing?.WidthMm > 0 ? existing.WidthMm : frameWidthMm + left + right;
        ThicknessText = $"{existing?.ThicknessMm ?? SillElement.StandardThicknessMm:0}";
        WidthText = $"{width:0}";
        LeftOverhangText = $"{left:0}";
        RightOverhangText = $"{right:0}";

        MaterialOptions = ConstructionMaterialCatalog.ForKind(GeometryElementKind.Sill)
            .OrderBy(material => material.Name)
            .Select(material => new SillMaterialOption(material.Id, material.Name, ResolveColor(material.Id)))
            .ToList();

        SelectedMaterial = MaterialOptions.FirstOrDefault(option => option.MaterialId == (existing?.MaterialId ?? SillElement.DefaultMaterialId))
            ?? MaterialOptions.FirstOrDefault();
    }

    [RelayCommand]
    private void Confirm()
    {
        ValidationMessage = string.Empty;

        if (!TryParsePositive(ThicknessText, out var thickness) || thickness < 10)
        {
            ValidationMessage = "Толщина должна быть не менее 10 мм";
            return;
        }

        if (!TryParsePositive(WidthText, out var width))
        {
            ValidationMessage = "Укажите корректную ширину подоконника";
            return;
        }

        if (!TryParseNonNegative(LeftOverhangText, out var leftOverhang))
        {
            ValidationMessage = "Укажите корректный вынос слева";
            return;
        }

        if (!TryParseNonNegative(RightOverhangText, out var rightOverhang))
        {
            ValidationMessage = "Укажите корректный вынос справа";
            return;
        }

        if (SelectedMaterial is null)
        {
            ValidationMessage = "Выберите материал подоконника";
            return;
        }

        ResultThicknessMm = thickness;
        ResultWidthMm = width;
        ResultLeftOverhangMm = leftOverhang;
        ResultRightOverhangMm = rightOverhang;
        ResultMaterialId = SelectedMaterial.MaterialId;
        ResultColor = SelectedMaterial.ColorHex;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);

    private static bool TryParsePositive(string text, out double value) =>
        double.TryParse(text.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value) && value > 0;

    private static bool TryParseNonNegative(string text, out double value) =>
        double.TryParse(text.Trim().Replace(',', '.'), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out value) && value >= 0;

    private static string ResolveColor(Guid materialId) =>
        materialId == SillElement.DefaultMaterialId
            ? SillElement.DefaultColorHex
            : materialId == Guid.Parse("b1000082-0000-4000-8000-000000000082")
                ? "#D4C4A8"
                : SillElement.DefaultColorHex;

    public sealed record SillMaterialOption(Guid MaterialId, string Name, string ColorHex);
}
