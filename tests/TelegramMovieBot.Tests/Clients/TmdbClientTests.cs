using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TelegramMovieBot.Api.Clients;
using TelegramMovieBot.Api.Options;

namespace TelegramMovieBot.Tests.Clients;

public sealed class TmdbClientTests
{
    [Fact]
    public async Task GetNowPlayingAsync_SendsExpectedRequestAndReadsResponse()
    {
        Uri? requestedUri = null;
        var handler = new StubHttpMessageHandler(request =>
        {
            requestedUri = request.RequestUri;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "page": 1,
                      "results": [{ "id": 42, "title": "Test Film", "adult": false }],
                      "total_pages": 3,
                      "total_results": 50
                    }
                    """,
                    System.Text.Encoding.UTF8,
                    "application/json")
            };
        });

        var client = CreateClient(handler);

        var result = await client.GetNowPlayingAsync();

        Assert.Equal(1, result.Page);
        Assert.Equal(42, Assert.Single(result.Results).Id);
        Assert.Equal(
            "https://api.themoviedb.org/3/movie/now_playing?language=tr-TR&region=TR&page=1",
            requestedUri?.ToString());
    }

    [Fact]
    public async Task GetUpcomingAsync_WithInvalidPage_ThrowsBeforeSendingRequest()
    {
        var handler = new StubHttpMessageHandler(_ =>
            throw new InvalidOperationException("HTTP isteği gönderilmemeliydi."));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            client.GetUpcomingAsync(page: 0));
    }

    [Fact]
    public async Task GetUpcomingAsync_WhenTmdbFails_ThrowsHttpRequestException()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized));
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetUpcomingAsync());
    }

    [Fact]
    public async Task GetNowPlayingAsync_WhenCancelled_StopsRequest()
    {
        var handler = new CancellableHttpMessageHandler();
        var client = CreateClient(handler);
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.GetNowPlayingAsync(cancellationToken: cancellationTokenSource.Token));
    }

    private static TmdbClient CreateClient(HttpMessageHandler handler)
    {
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

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }

    private sealed class CancellableHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }
}
