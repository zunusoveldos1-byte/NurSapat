using Avalonia.Controls;
using Avalonia.Input; // �������� using ��� PointerPressedEventArgs

namespace NurSapat.App.Views.Dialogs;

public partial class UnsavedChangesWindow : Window
{
    public UnsavedChangesWindow()
    {
        InitializeComponent();
    }

    // ����� ��� �������������� ���� ������
    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }
}