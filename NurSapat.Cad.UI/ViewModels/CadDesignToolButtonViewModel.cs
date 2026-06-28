using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    SlantedImpost,
    Selection,
    UndoLast,
    UndoGlobal,
    SelectObject,
    SelectProfile,
    SelectVertex,
    Zoom,
    Hand,
    ZoomFit,
    ArchHeight,
    Resize,
    Trash,
    ClearAll,
    MoveMullionVertical,
    MoveMullionHorizontal,
    AlignMullions,
    AlignGlassUnits,
    AlignClearOpenings
}

public sealed partial class CadDesignToolButtonViewModel : ObservableObject
{
    public CadDesignToolButtonViewModel(
        WindowToolMode mode,
        string title,
        DesignToolIcon icon = DesignToolIcon.FakeFrame,
        IRelayCommand? command = null,
        bool isPlaceholder = false,
        bool isSeparator = false)
    {
        Mode = mode;
        Title = title;
        Icon = icon;
        Command = command;
        IsPlaceholder = isPlaceholder;
        IsSeparator = isSeparator;
    }

    public WindowToolMode Mode { get; }

    public string Title { get; }

    public DesignToolIcon Icon { get; }

    public IRelayCommand? Command { get; }

    public bool IsCommandButton => Command != null;

    public bool IsModeButton => !IsSeparator && !IsCommandButton;

    public bool IsPlaceholder { get; }

    public bool IsSeparator { get; }

    [ObservableProperty]
    private bool _isActive;
}
