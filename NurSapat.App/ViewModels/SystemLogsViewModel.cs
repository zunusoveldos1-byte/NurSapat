using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NurSapat.Core.Interfaces;
using NurSapat.Core.Models;
using NurSapat.App.Services.Users;
using NurSapat.Infrastructure.Sync.Diagnostics;

namespace NurSapat.App.ViewModels;

public partial class SystemLogsViewModel : ObservableObject
{
    private readonly ISystemLogRepository _systemLogRepository;
    private readonly IUserSessionService _userSessionService;

    [ObservableProperty]
    private ObservableCollection<SystemLogEntry> _logs = new();

    [ObservableProperty]
    private string _statusMessage = "Журнал готов к загрузке.";

    [ObservableProperty]
    private bool _isBusy;

    public SystemLogsViewModel(
        ISystemLogRepository systemLogRepository,
        IUserSessionService userSessionService)
    {
        _systemLogRepository = systemLogRepository;
        _userSessionService = userSessionService;
        if (_userSessionService.IsOffline)
        {
            StatusMessage = "Системный журнал недоступен в оффлайн-режиме.";
            StartupTrace.Info(
                "[LOG] SystemLogsViewModel: оффлайн-режим, PostgreSQL не вызывается.");
        }
        else
        {
            _ = LoadAsync();
        }
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_userSessionService.IsOffline)
        {
            Logs.Clear();
            StatusMessage = "Системный журнал недоступен в оффлайн-режиме.";
            StartupTrace.Info(
                "[LOG] SystemLogsViewModel.LoadAsync: оффлайн-режим, PostgreSQL не вызывается.");
            return;
        }

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
            StartupTrace.Error("[LOG] SystemLogsViewModel.LoadAsync", ex);
            StatusMessage = $"Не удалось загрузить журнал: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
