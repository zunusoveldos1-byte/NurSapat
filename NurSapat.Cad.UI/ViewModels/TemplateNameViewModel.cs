using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class TemplateNameViewModel : ObservableObject
{
    public event EventHandler<bool>? CloseRequested;

    [ObservableProperty]
    private string _nameText = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    public string ResultName { get; private set; } = string.Empty;

    public TemplateNameViewModel(string initialName) => NameText = initialName;

    [RelayCommand]
    private void Confirm()
    {
        ValidationMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(NameText))
        {
            ValidationMessage = "Укажите наименование шаблона";
            return;
        }

        ResultName = NameText.Trim();
        CloseRequested?.Invoke(this, true);
    }

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, false);
}
