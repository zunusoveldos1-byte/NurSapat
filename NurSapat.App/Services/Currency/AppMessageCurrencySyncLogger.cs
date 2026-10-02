using NurSapat.App.Services;
using NurSapat.Core.Interfaces.Currency;

namespace NurSapat.App.Services.Currency;

public sealed class AppMessageCurrencySyncLogger : ICurrencySyncLogger
{
    private readonly IAppMessageService _messageService;

    public AppMessageCurrencySyncLogger(IAppMessageService messageService)
    {
        _messageService = messageService;
    }

    public void LogWarning(string message) => _messageService.LogWarning(message);
}
