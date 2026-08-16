using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TelegramMovieBot.Api.Exceptions;
using TelegramMovieBot.Api.Models.Telegram;
using TelegramMovieBot.Api.Options;

namespace TelegramMovieBot.Api.Clients;

public sealed class TelegramClient(
    HttpClient httpClient,
    IOptions<TelegramOptions> options,
    ILogger<TelegramClient> logger)
{
    private const int MaximumMessageLength = 4096;
    private readonly TelegramOptions _options = options.Value;

    public async Task<TelegramMessage> SendMessageAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BotToken))
        {
            throw new TelegramConfigurationException(
                "Telegram bot token tanımlanmamış.");
        }

        if (string.IsNullOrWhiteSpace(_options.ChatId))
        {
            throw new TelegramConfigurationException(
                "Telegram chat kimliği tanımlanmamış.");
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Telegram mesajı boş olamaz.", nameof(text));
        }

        if (text.Length > MaximumMessageLength)
        {
            throw new ArgumentException(
                $"Telegram mesajı {MaximumMessageLength} karakteri aşamaz.",
                nameof(text));
        }

        var request = new TelegramSendMessageRequest(_options.ChatId, text);
        var requestUri = $"./bot{_options.BotToken}/sendMessage";

        logger.LogInformation("Telegram mesajı gönderiliyor.");

        using var response = await httpClient.PostAsJsonAsync(
            requestUri,
            request,
            cancellationToken);

        TelegramApiResponse<TelegramMessage>? telegramResponse;

        try
        {
            telegramResponse = await response.Content
                .ReadFromJsonAsync<TelegramApiResponse<TelegramMessage>>(
                    cancellationToken: cancellationToken);
        }
        catch (System.Text.Json.JsonException)
        {
            throw new TelegramApiException(
                "Telegram geçersiz bir cevap döndürdü.",
                errorCode: (int)response.StatusCode);
        }

        if (!response.IsSuccessStatusCode || telegramResponse is not { Ok: true, Result: not null })
        {
            throw new TelegramApiException(
                telegramResponse?.Description ?? "Telegram mesajı gönderilemedi.",
                telegramResponse?.ErrorCode ?? (int)response.StatusCode);
        }

        logger.LogInformation(
            "Telegram mesajı başarıyla gönderildi. Mesaj kimliği: {MessageId}",
            telegramResponse.Result.MessageId);

        return telegramResponse.Result;
    }
}
