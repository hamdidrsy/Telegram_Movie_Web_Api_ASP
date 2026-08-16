using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TelegramMovieBot.Api.Clients;
using TelegramMovieBot.Api.Options;
using TelegramMovieBot.Api.Services;

namespace TelegramMovieBot.Tests.Services;

public sealed class MovieServiceTests
{
    [Fact]
    public async Task GetNowPlayingAsync_FiltersAdultAndSortsByPopularity()
    {
        var service = CreateService(
            """
            {
              "page": 1,
              "results": [
                { "id": 1, "title": "Az Popüler", "popularity": 10, "adult": false },
                { "id": 2, "title": "Yetişkin", "popularity": 100, "adult": true },
                { "id": 3, "title": "Çok Popüler", "popularity": 50, "adult": false }
              ],
              "total_pages": 1,
              "total_results": 3
            }
            """);

        var movies = await service.GetNowPlayingAsync();

        Assert.Collection(
            movies,
            movie => Assert.Equal(3, movie.Id),
            movie => Assert.Equal(1, movie.Id));
    }

    [Fact]
    public async Task GetUpcomingAsync_SortsByReleaseDateAndMapsMissingValues()
    {
        var service = CreateService(
            """
            {
              "page": 1,
              "results": [
                {
                  "id": 10,
                  "title": "",
                  "original_title": "Original Name",
                  "overview": null,
                  "poster_path": null,
                  "release_date": "2026-10-10",
                  "adult": false
                },
                {
                  "id": 20,
                  "title": "Önce Çıkacak",
                  "release_date": "2026-09-01",
                  "poster_path": "/poster.jpg",
                  "adult": false
                }
              ],
              "total_pages": 1,
              "total_results": 2
            }
            """);

        var movies = await service.GetUpcomingAsync();

        Assert.Equal(20, movies[0].Id);
        Assert.Equal("https://image.tmdb.org/t/p/w500/poster.jpg", movies[0].PosterUrl);
        Assert.Equal("Original Name", movies[1].Title);
        Assert.Equal(string.Empty, movies[1].Overview);
        Assert.Null(movies[1].PosterUrl);
        Assert.Equal(new DateOnly(2026, 10, 10), movies[1].ReleaseDate);
    }

    private static MovieService CreateService(string json)
    {
        var handler = new StubHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                json,
                System.Text.Encoding.UTF8,
                "application/json")
        });
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3/")
        };
        var options = Microsoft.Extensions.Options.Options.Create(new TmdbOptions
        {
            AccessToken = "test-token"
        });
        var tmdbClient = new TmdbClient(
            httpClient,
            options,
            NullLogger<TmdbClient>.Instance);

        return new MovieService(tmdbClient);
    }

    private sealed class StubHttpMessageHandler(HttpResponseMessage response)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
