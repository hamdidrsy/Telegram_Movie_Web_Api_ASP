using System.Text.Json.Serialization;

namespace TelegramMovieBot.Api.Models.Telegram;

public sealed class TelegramMessage
{
    [JsonPropertyName("message_id")]
    public long MessageId { get; init; }

    [JsonPropertyName("date")]
    public long Date { get; init; }

    [JsonPropertyName("text")]
    public string? Text { get; init; }
}
