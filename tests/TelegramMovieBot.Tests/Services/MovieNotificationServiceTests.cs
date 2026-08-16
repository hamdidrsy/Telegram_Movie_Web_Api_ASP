using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using TelegramMovieBot.Api.Clients;
using TelegramMovieBot.Api.Options;
using TelegramMovieBot.Api.Services;

namespace TelegramMovieBot.Tests.Services;

public sealed class MovieNotificationServiceTests
{
    [Fact]
    public async Task SendMovieNotificationAsync_SendsNowPlayingAndUpcomingMessages()
    {
        var tmdbClient = CreateTmdbClient();
        var movieService = new MovieService(tmdbClient);
        var notificationOptions = Microsoft.Extensions.Options.Options.Create(
            new NotificationOptions { MaxMoviesPerList = 8 });
        var formatter = new TelegramMessageFormatter(notificationOptions);
        var telegramHandler = new TelegramStubHandler();
        var telegramClient = CreateTelegramClient(telegramHandler);
        var service = new MovieNotificationService(
            movieService,
            formatter,
            telegramClient,
            NullLogger<MovieNotificationService>.Instance);

        var sentMessageCount = await service.SendMovieNotificationAsync();

        Assert.Equal(2, sentMessageCount);
        Assert.Equal(2, telegramHandler.MessageBodies.Count);
        Assert.Contains("Vizyondaki Filmler", telegramHandler.MessageBodies[0]);
        Assert.Contains("Vizyondaki Test Filmi", telegramHandler.MessageBodies[0]);
        Assert.Contains("Yakında Vizyona Girecek Filmler", telegramHandler.MessageBodies[1]);
        Assert.Contains("Gelecek Test Filmi", telegramHandler.MessageBodies[1]);
    }

    private static TmdbClient CreateTmdbClient()
    {
        var handler = new TmdbStubHandler();
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3/")
        };
        var options = Microsoft.Extensions.Options.Options.Create(new TmdbOptions
        {
            AccessToken = "test-token"
        });

        return new TmdbClient(
            httpClient,
            options,
            NullLogger<TmdbClient>.Instance);
    }

    private static TelegramClient CreateTelegramClient(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.telegram.org/")
        };
        var options = Microsoft.Extensions.Options.Options.Create(new TelegramOptions
        {
            BotToken = "123456789:test-token",
            ChatId = "test-chat"
        });

        return new TelegramClient(
            httpClient,
            options,
            NullLogger<TelegramClient>.Instance);
    }

    private sealed class TmdbStubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var isUpcoming = request.RequestUri?.AbsolutePath.Contains("upcoming") == true;
            var title = isUpcoming ? "Gelecek Test Filmi" : "Vizyondaki Test Filmi";
            var id = isUpcoming ? 2 : 1;
            var json = $$"""
                {
                  "page": 1,
                  "results": [
                    {
                      "id": {{id}},
                      "title": "{{title}}",
                      "release_date": "2026-08-16",
                      "vote_average": 8.0,
                      "popularity": 100,
                      "adult": false
                    }
                  ],
                  "total_pages": 1,
                  "total_results": 1
                }
                """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    json,
                    System.Text.Encoding.UTF8,
                    "application/json")
            });
        }
    }

    private sealed class TelegramStubHandler : HttpMessageHandler
    {
        public List<string> MessageBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var requestBody = await request.Content!
                .ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(requestBody);
            MessageBodies.Add(
                document.RootElement.GetProperty("text").GetString()!);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    { "ok": true, "result": { "message_id": 1, "date": 1 } }
                    """,
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
