using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.App.ViewModels.Dialogs;

public sealed partial class InputNumberDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Ввод числа";

    [ObservableProperty]
    private string _prompt = "Введите значение:";

    [ObservableProperty]
    private decimal _value = 1;

    [ObservableProperty]
    private decimal _minimum = 1;

    [ObservableProperty]
    private decimal _maximum = int.MaxValue;

    public event Action<int?>? RequestClose;

    [RelayCommand]
    private void Confirm()
    {
        if (Value < Minimum || Value > Maximum)
        {
            return;
        }

        RequestClose?.Invoke((int)Value);
    }

    [RelayCommand]
    private void Cancel() => RequestClose?.Invoke(null);
}
