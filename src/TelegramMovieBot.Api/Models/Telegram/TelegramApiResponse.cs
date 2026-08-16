using System.Text.Json.Serialization;

namespace TelegramMovieBot.Api.Models.Telegram;

public sealed class TelegramApiResponse<T>
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("result")]
    public T? Result { get; init; }

    [JsonPropertyName("error_code")]
    public int? ErrorCode { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }
}
