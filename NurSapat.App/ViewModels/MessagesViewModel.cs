using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.App.Models.View;
using NurSapat.App.Services;
using System.Collections.ObjectModel;

namespace NurSapat.App.ViewModels;

public sealed partial class MessagesViewModel : ViewModelBase
{
    private readonly IAppMessageService _messageService;

    public ObservableCollection<MessageItemViewModel> FilteredMessages { get; } = [];

    [ObservableProperty]
    private bool _showErrors = true;

    [ObservableProperty]
    private bool _showWarnings = true;

    public MessagesViewModel(IAppMessageService messageService)
    {
        _messageService = messageService;
        _messageService.MessagesChanged += OnMessagesChanged;
        RebuildFilteredMessages();
    }

    partial void OnShowErrorsChanged(bool value) => RebuildFilteredMessages();

    partial void OnShowWarningsChanged(bool value) => RebuildFilteredMessages();

    [RelayCommand]
    private void ClearAll() => _messageService.Clear();

    private void OnMessagesChanged(object? sender, EventArgs e) => RebuildFilteredMessages();

    private void RebuildFilteredMessages()
    {
        FilteredMessages.Clear();

        foreach (var message in _messageService.Messages)
        {
            if (!ShouldDisplay(message.Severity))
            {
                continue;
            }

            FilteredMessages.Add(new MessageItemViewModel(message));
        }
    }

    private bool ShouldDisplay(AppMessageSeverity severity) => severity switch
    {
        AppMessageSeverity.Error => ShowErrors,
        AppMessageSeverity.Warning => ShowWarnings,
        _ => true
    };
}
