using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadToolButtonViewModel(string id, string displayName) : ObservableObject
{
    public string Id { get; } = id;
    public string DisplayName { get; } = displayName;

    [ObservableProperty]
    private bool _isActive;
}