using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.App.ViewModels;

public sealed partial class WorkspaceSectionViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _description;

    [ObservableProperty]
    private ObservableCollection<string> _workItems;

    public WorkspaceSectionViewModel(string title, string description, IEnumerable<string> workItems)
    {
        Title = title;
        Description = description;
        WorkItems = new ObservableCollection<string>(workItems);
    }
}
