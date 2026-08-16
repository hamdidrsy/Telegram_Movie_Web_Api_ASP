using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TelegramMovieBot.Api.Clients;
using TelegramMovieBot.Api.Exceptions;
using TelegramMovieBot.Api.Options;

namespace TelegramMovieBot.Tests.Clients;

public sealed class TelegramClientTests
{
    [Fact]
    public async Task SendMessageAsync_SendsExpectedPostRequest()
    {
        HttpMethod? method = null;
        Uri? requestUri = null;
        string? requestBody = null;
        var handler = new StubHttpMessageHandler(async (request, cancellationToken) =>
        {
            method = request.Method;
            requestUri = request.RequestUri;
            requestBody = await request.Content!.ReadAsStringAsync(cancellationToken);

            return CreateResponse(HttpStatusCode.OK,
                """
                { "ok": true, "result": { "message_id": 99, "date": 1, "text": "Merhaba" } }
                """);
        });
        var client = CreateClient(handler);

        var result = await client.SendMessageAsync("Merhaba");

        Assert.Equal(HttpMethod.Post, method);
        Assert.Equal(
            "https://api.telegram.org/bot123456789:test-bot-token/sendMessage",
            requestUri?.ToString());
        Assert.Equal(99, result.MessageId);

        using var json = JsonDocument.Parse(requestBody!);
        Assert.Equal("test-chat-id", json.RootElement.GetProperty("chat_id").GetString());
        Assert.Equal("Merhaba", json.RootElement.GetProperty("text").GetString());
    }

    [Fact]
    public async Task SendMessageAsync_WithoutToken_DoesNotSendRequest()
    {
        var requestSent = false;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            requestSent = true;
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, "{}"));
        });
        var client = CreateClient(handler, botToken: string.Empty);

        await Assert.ThrowsAsync<TelegramConfigurationException>(() =>
            client.SendMessageAsync("Merhaba"));

        Assert.False(requestSent);
    }

    [Fact]
    public async Task SendMessageAsync_WithoutChatId_DoesNotSendRequest()
    {
        var requestSent = false;
        var handler = new StubHttpMessageHandler((_, _) =>
        {
            requestSent = true;
            return Task.FromResult(CreateResponse(HttpStatusCode.OK, "{}"));
        });
        var client = CreateClient(handler, chatId: string.Empty);

        await Assert.ThrowsAsync<TelegramConfigurationException>(() =>
            client.SendMessageAsync("Merhaba"));

        Assert.False(requestSent);
    }

    [Fact]
    public async Task SendMessageAsync_WhenTelegramRejects_ThrowsApiException()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(CreateResponse(
                HttpStatusCode.BadRequest,
                """
                { "ok": false, "error_code": 400, "description": "Bad Request" }
                """)));
        var client = CreateClient(handler);

        var exception = await Assert.ThrowsAsync<TelegramApiException>(() =>
            client.SendMessageAsync("Merhaba"));

        Assert.Equal(400, exception.ErrorCode);
    }

    [Fact]
    public async Task SendMessageAsync_WhenCancelled_StopsRequest()
    {
        var handler = new StubHttpMessageHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return CreateResponse(HttpStatusCode.OK, "{}");
        });
        var client = CreateClient(handler);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.SendMessageAsync(
                "Merhaba",
                cancellationTokenSource.Token));
    }

    private static TelegramClient CreateClient(
        HttpMessageHandler handler,
        string botToken = "123456789:test-bot-token",
        string chatId = "test-chat-id")
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.telegram.org/")
        };
        var options = Microsoft.Extensions.Options.Options.Create(new TelegramOptions
        {
            BotToken = botToken,
            ChatId = chatId
        });

        return new TelegramClient(
            httpClient,
            options,
            NullLogger<TelegramClient>.Instance);
    }

    private static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json")
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responseFactory(request, cancellationToken);
    }
}
