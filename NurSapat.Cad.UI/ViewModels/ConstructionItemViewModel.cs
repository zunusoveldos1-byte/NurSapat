using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class ConstructionItemViewModel : ObservableObject
{
    public ConstructionItemViewModel(int id, string name)
    {
        Id = id;
        Name = name;
    }

    public int Id { get; }
    public string Name { get; }

    public override string ToString() => Name;
}
