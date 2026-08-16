using System.Text.Json.Serialization;

namespace TelegramMovieBot.Api.Models.Telegram;

public sealed record TelegramSendMessageRequest(
    [property: JsonPropertyName("chat_id")] string ChatId,
    [property: JsonPropertyName("text")] string Text,
    [property: JsonPropertyName("parse_mode")] string ParseMode = "HTML",
    [property: JsonPropertyName("disable_web_page_preview")] bool DisableWebPagePreview = true);
