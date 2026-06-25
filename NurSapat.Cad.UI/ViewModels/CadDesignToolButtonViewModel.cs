using CommunityToolkit.Mvvm.ComponentModel;
using NurSapat.Cad.Core.Models;

namespace NurSapat.Cad.UI.ViewModels;

public enum DesignToolIcon
{
    FakeFrame,
    FakeCircle,
    FakeTriangle,
    FakeArch,
    VerticalImpost,
    HorizontalImpost,
    RackRigel,
    RealFrame,
    AdapterProfile,
    Sash,
    Hardware,
    Handle,
    RealVerticalImpost,
    RealHorizontalImpost,
    DoubleVerticalImpost,
    DoubleHorizontalImpost,
    SlantedImpost
}

public sealed partial class CadDesignToolButtonViewModel : ObservableObject
{
    public CadDesignToolButtonViewModel(
        WindowToolMode mode,
        string title,
        DesignToolIcon icon = DesignToolIcon.FakeFrame,
        bool isPlaceholder = false,
        bool isSeparator = false)
    {
        Mode = mode;
        Title = title;
        Icon = icon;
        IsPlaceholder = isPlaceholder;
        IsSeparator = isSeparator;
    }

    public WindowToolMode Mode { get; }

    public string Title { get; }

    public DesignToolIcon Icon { get; }

    public bool IsPlaceholder { get; }

    public bool IsSeparator { get; }

    [ObservableProperty]
    private bool _isActive;
}
