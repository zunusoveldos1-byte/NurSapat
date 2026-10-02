using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.App.ViewModels.Dictionaries;

public abstract partial class DictionaryItemViewModelBase : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
