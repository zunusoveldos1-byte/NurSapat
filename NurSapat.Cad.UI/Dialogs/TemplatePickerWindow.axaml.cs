using Avalonia.Controls;
using Avalonia.Input; // ����������� �������� ���� using

namespace NurSapat.Cad.UI.Dialogs;

public partial class TemplatePickerWindow : Window
{
    public TemplatePickerWindow()
    {
        InitializeComponent();
    }

    // ��������� ���� �����, ����� ���� ����� ���� �������������
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}