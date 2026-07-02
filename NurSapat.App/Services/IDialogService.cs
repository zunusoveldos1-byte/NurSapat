namespace NurSapat.App.Services;

public enum UnsavedChangesChoice
{
    Cancel,
    Save,
    SaveAsCopy,
    Discard
}

public enum YesNoChoice
{
    No,
    Yes
}

public interface IDialogService
{
    Task<UnsavedChangesChoice> ShowUnsavedChangesDialogAsync(
        string title,
        string message,
        CancellationToken cancellationToken = default);

    Task<YesNoChoice> ShowAutoSaveRecoveryDialogAsync(CancellationToken cancellationToken = default);

    Task<string?> ShowSaveFileDialogAsync(
        string suggestedFileName,
        CancellationToken cancellationToken = default);

    Task<string?> ShowOpenFileDialogAsync(CancellationToken cancellationToken = default);

    Task ShowErrorAsync(string message, CancellationToken cancellationToken = default);

    Task<bool> ShowPrintDialogAsync(ViewModels.PrintDialogViewModel viewModel, CancellationToken cancellationToken = default);

    Task<bool> ShowPrintSettingsDialogAsync(ViewModels.PrintSettingsViewModel viewModel, CancellationToken cancellationToken = default);

    Task<bool> ShowCopyDrawingDialogAsync(ViewModels.Dialogs.Edit.CopyDrawingDialogViewModel viewModel, CancellationToken cancellationToken = default);

    Task<bool> ShowConstructionColorDialogAsync(ViewModels.Dialogs.Edit.ConstructionColorDialogViewModel viewModel, CancellationToken cancellationToken = default);

    Task<bool> ShowOrderColorDialogAsync(ViewModels.Dialogs.Edit.OrderColorDialogViewModel viewModel, CancellationToken cancellationToken = default);

    Task<string?> ShowListPickerAsync(
        string title,
        IReadOnlyList<string> items,
        CancellationToken cancellationToken = default);
}
