using System.Text.Json;
using TelegramMovieBot.Api.Models.Telegram;

namespace TelegramMovieBot.Tests.Models;

public sealed class TelegramSendMessageRequestTests
{
    [Fact]
    public void Serialize_UsesTelegramFieldNamesAndDefaults()
    {
        var request = new TelegramSendMessageRequest("12345", "Test mesajı");

        var json = JsonSerializer.Serialize(request);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal("12345", root.GetProperty("chat_id").GetString());
        Assert.Equal("Test mesajı", root.GetProperty("text").GetString());
        Assert.Equal("HTML", root.GetProperty("parse_mode").GetString());
        Assert.True(root.GetProperty("disable_web_page_preview").GetBoolean());
    }

    [Fact]
    public void Deserialize_ReadsSuccessfulTelegramResponse()
    {
        const string json =
            """
            {
              "ok": true,
              "result": {
                "message_id": 77,
                "date": 1750000000,
                "text": "Test mesajı"
              }
            }
            """;

        var response = JsonSerializer.Deserialize<TelegramApiResponse<TelegramMessage>>(json);

        Assert.NotNull(response);
        Assert.True(response.Ok);
        Assert.Equal(77, response.Result?.MessageId);
        Assert.Equal("Test mesajı", response.Result?.Text);
    }
}
