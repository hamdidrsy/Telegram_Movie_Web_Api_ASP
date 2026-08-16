using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TelegramMovieBot.Api.Exceptions;
using TelegramMovieBot.Api.Models;
using TelegramMovieBot.Api.Options;

namespace TelegramMovieBot.Api.Clients;

public sealed class TmdbClient(
    HttpClient httpClient,
    IOptions<TmdbOptions> options,
    ILogger<TmdbClient> logger)
{
    private readonly TmdbOptions _options = options.Value;

    public Task<TmdbMovieResponse> GetNowPlayingAsync(
        int page = 1,
        CancellationToken cancellationToken = default) =>
        GetMoviesAsync("movie/now_playing", page, cancellationToken);

    public Task<TmdbMovieResponse> GetUpcomingAsync(
        int page = 1,
        CancellationToken cancellationToken = default) =>
        GetMoviesAsync("movie/upcoming", page, cancellationToken);

    private async Task<TmdbMovieResponse> GetMoviesAsync(
        string endpoint,
        int page,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);

        if (string.IsNullOrWhiteSpace(_options.AccessToken))
        {
            throw new TmdbConfigurationException(
                "TMDB Access Token tanımlanmamış. Tmdb:AccessToken ayarını kontrol edin.");
        }

        var requestUri =
            $"{endpoint}?language={Uri.EscapeDataString(_options.Language)}" +
            $"&region={Uri.EscapeDataString(_options.Region)}&page={page}";

        logger.LogInformation(
            "TMDB film listesi isteniyor. Endpoint: {Endpoint}, Sayfa: {Page}",
            endpoint,
            page);

        using var response = await httpClient.GetAsync(
            requestUri,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TmdbMovieResponse>(
            cancellationToken: cancellationToken);

        return result ?? throw new TmdbInvalidResponseException(
            "TMDB boş veya geçersiz bir cevap döndürdü.");
    }
}
