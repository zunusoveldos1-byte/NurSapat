using Avalonia.Controls;
using Avalonia.Interactivity;

namespace NurSapat.Cad.UI.Dialogs;

public partial class SizeInputWindow : Window
{
    public double ResultWidth { get; private set; }
    public double ResultHeight { get; private set; }
    public bool IsConfirmed { get; private set; }

    public SizeInputWindow()
    {
        InitializeComponent();
    }

    public void SetInitialValues(double width, double height)
    {
        WidthInput.Value = (decimal)width;
        HeightInput.Value = (decimal)height;
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        ResultWidth = (double)(WidthInput.Value ?? 1000m);
        ResultHeight = (double)(HeightInput.Value ?? 1000m);
        Close(true); // ← обязательно!
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        IsConfirmed = false;
        Close(false);
    }
}