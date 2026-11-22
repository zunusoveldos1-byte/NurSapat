using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.UI.Models;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadToolbarItemViewModel : ObservableObject
{
    public CadToolbarItemViewModel(CadToolbarItem item)
    {
        Item = item;
        Hint = item.Hint;
        IconGlyph = item.IconGlyph;
        Mode = item.Mode;
    }

    public CadToolbarItem Item { get; }

    public string Hint { get; }
    public string IconGlyph { get; }
    public WindowToolMode Mode { get; }

    [ObservableProperty]
    private bool _isActive;
}
