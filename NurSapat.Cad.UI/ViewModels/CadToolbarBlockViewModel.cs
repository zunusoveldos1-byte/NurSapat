using CommunityToolkit.Mvvm.ComponentModel;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadToolbarBlockViewModel : ObservableObject
{
    public CadToolbarBlockViewModel(string title, IReadOnlyList<CadToolbarItemViewModel> tools)
    {
        Title = title;
        Tools = tools;
    }

    public string Title { get; }
    public IReadOnlyList<CadToolbarItemViewModel> Tools { get; }
}
