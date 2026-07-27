using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.App.Models.Config;

namespace NurSapat.App.ViewModels.Config;

public sealed class ConstructionElementItemViewModel : ObservableObject
{
    private string _name = string.Empty;

    public ConstructionElementItemViewModel(ConstructionElement source)
    {
        Id = source.Id;
        ManufacturerId = source.ManufacturerId;
        Section = source.Section;
        Name = source.Name;
    }

    public Guid Id { get; }

    public Guid ManufacturerId { get; }

    public ConstructionSectionType Section { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
