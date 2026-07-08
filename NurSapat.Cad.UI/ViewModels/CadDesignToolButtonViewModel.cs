using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Cad.Core.Models;
using NurSapat.Cad.Tools;

namespace NurSapat.Cad.UI.ViewModels;

public sealed partial class CadDesignToolButtonViewModel : ObservableObject
{
    public static CadDesignToolButtonViewModel CreateBlockHeader(string title) =>
        new("block:" + title, WindowToolMode.Separator, title, isBlockHeader: true);

    public static CadDesignToolButtonViewModel CreateSectionHeader(string title) =>
        new("section:" + title, WindowToolMode.Separator, title, isSectionHeader: true);

    public static CadDesignToolButtonViewModel CreateSectionSpacer() =>
        new("spacer", WindowToolMode.Separator, string.Empty, isSectionSpacer: true);

    public static CadDesignToolButtonViewModel CreateBlockSpacer() =>
        new("block-spacer", WindowToolMode.Separator, string.Empty, isBlockSpacer: true);

    public CadDesignToolButtonViewModel(
        string toolKey,
        WindowToolMode mode,
        string title,
        string? iconResourceKey = null,
        IRelayCommand<string>? activateCommand = null,
        CadToolKind? cadToolKind = null,
        bool isInstantCommand = false,
        bool isPlaceholder = false,
        bool isSeparator = false,
        bool isBlockHeader = false,
        bool isSectionHeader = false,
        bool isSectionSpacer = false,
        bool isBlockSpacer = false,
        bool isImplemented = true)
    {
        ToolKey = toolKey;
        Mode = mode;
        Title = title;
        IconResourceKey = iconResourceKey;
        ActivateCommand = activateCommand;
        CadToolKind = cadToolKind;
        IsInstantCommand = isInstantCommand;
        IsPlaceholder = isPlaceholder;
        IsSeparator = isSeparator;
        IsBlockHeader = isBlockHeader;
        IsSectionHeader = isSectionHeader;
        IsSectionSpacer = isSectionSpacer;
        IsBlockSpacer = isBlockSpacer;
        IsImplemented = isImplemented;
    }

    public string ToolKey { get; }

    public WindowToolMode Mode { get; }

    public string Title { get; }

    /// <summary>Resource key pointing to a <c>StreamGeometry</c> in CadToolIcons.axaml.</summary>
    public string? IconResourceKey { get; }

    public IRelayCommand<string>? ActivateCommand { get; }

    public CadToolKind? CadToolKind { get; }

    public bool IsInstantCommand { get; }

    public bool IsModeButton => IsTool && !IsInstantCommand;

    public bool IsTool => !IsSeparator && !IsBlockHeader && !IsSectionHeader && !IsSectionSpacer && !IsBlockSpacer;

    public bool IsPlaceholder { get; }

    public bool IsSeparator { get; }

    public bool IsBlockHeader { get; }

    public bool IsSectionHeader { get; }

    public bool IsSectionSpacer { get; }

    public bool IsBlockSpacer { get; }

    public bool IsImplemented { get; }

    public string TooltipText => IsImplemented
        ? Title
        : "Функция пока не реализована.";

    [ObservableProperty]
    private bool _isActive;
}
