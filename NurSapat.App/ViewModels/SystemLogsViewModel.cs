using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models;

namespace NurSapat.App.ViewModels;

public partial class SystemLogsViewModel : ObservableObject
{
    private readonly ISystemLogRepository _systemLogRepository;

    [ObservableProperty]
    private ObservableCollection<SystemLogEntry> _logs = new();

    [ObservableProperty]
    private string _statusMessage = "Журнал готов к загрузке.";

    [ObservableProperty]
    private bool _isBusy;

    public SystemLogsViewModel(ISystemLogRepository systemLogRepository)
    {
        _systemLogRepository = systemLogRepository;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        StatusMessage = "Загрузка журнала...";

        try
        {
            var latest = await _systemLogRepository.GetLatestAsync();
            Logs = new ObservableCollection<SystemLogEntry>(latest);
            StatusMessage = Logs.Count == 0
                ? "В системном журнале пока нет записей."
                : $"Загружено записей: {Logs.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Не удалось загрузить журнал: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
