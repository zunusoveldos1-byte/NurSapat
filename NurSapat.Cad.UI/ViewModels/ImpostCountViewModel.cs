using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class ImpostCountViewModel : ObservableObject
{
    public event EventHandler<bool>? CloseRequested;

    [ObservableProperty]
    private string _countText = "1";

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public int ResultCount { get; private set; } = 1;

    public bool IsConfirmed { get; private set; }

    public ImpostCountViewModel(int initialCount = 1)
    {
        CountText = Math.Max(1, initialCount).ToString();
    }

    [RelayCommand]
    private void Confirm()
    {
        ValidationMessage = string.Empty;

        if (!int.TryParse(CountText.Trim(), out var count) || count < 1)
        {
            ValidationMessage = "Количество импостов должно быть целым числом ≥ 1";
            return;
        }

        ResultCount = count;
        IsConfirmed = true;
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel()
    {
        IsConfirmed = false;
        CloseRequested?.Invoke(this, false);
    }
}
