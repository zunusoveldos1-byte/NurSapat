using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using NurSapat.Infrastructure.Updates.Abstractions;
using NurSapat.Infrastructure.Updates.Options;

namespace NurSapat.Infrastructure.Updates.Internal;

public sealed class UpdateHttpClientFactory
{
    private readonly UpdateOptions _options;
    private readonly IUpdateLogger _logger;
    private readonly object _sync = new();
    private HttpClient? _client;

    public UpdateHttpClientFactory(IOptions<UpdateOptions> options, IUpdateLogger logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public HttpClient GetClient()
    {
        if (_client is not null)
        {
            return _client;
        }

        lock (_sync)
        {
            if (_client is not null)
            {
                return _client;
            }

            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            _client.DefaultRequestHeaders.UserAgent.ParseAdd(_options.UserAgent);
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
            return _client;
        }
    }

    public async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpClient, CancellationToken, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        const int maxAttempts = 4;
        var delay = TimeSpan.FromSeconds(1);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var response = await send(GetClient(), cancellationToken);
                if (IsRetriableStatus(response.StatusCode) && attempt < maxAttempts)
                {
                    _logger.Warning($"HTTP {(int)response.StatusCode} — повтор запроса ({attempt}/{maxAttempts}).");
                    response.Dispose();
                    await Task.Delay(delay, cancellationToken);
                    delay += delay;
                    continue;
                }

                return response;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt >= maxAttempts)
                {
                    throw new TimeoutException("Превышено время ожидания ответа сервера обновлений.");
                }

                _logger.Warning($"Таймаут HTTP — повтор запроса ({attempt}/{maxAttempts}).");
                await Task.Delay(delay, cancellationToken);
                delay += delay;
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                _logger.Warning($"Ошибка сети: {ex.Message} — повтор запроса ({attempt}/{maxAttempts}).");
                await Task.Delay(delay, cancellationToken);
                delay += delay;
            }
        }

        throw new InvalidOperationException("Не удалось выполнить HTTP-запрос.");
    }

    private static bool IsRetriableStatus(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
}
