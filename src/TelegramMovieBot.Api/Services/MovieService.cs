using System.Globalization;
using TelegramMovieBot.Api.Clients;
using TelegramMovieBot.Api.Models;

namespace TelegramMovieBot.Api.Services;

public sealed class MovieService(TmdbClient tmdbClient)
{
    private const string PosterBaseUrl = "https://image.tmdb.org/t/p/w500";
    private const string MovieBaseUrl = "https://www.themoviedb.org/movie";

    public async Task<IReadOnlyList<Movie>> GetNowPlayingAsync(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var response = await tmdbClient.GetNowPlayingAsync(page, cancellationToken);

        return response.Results
            .Where(movie => !movie.Adult)
            .OrderByDescending(movie => movie.Popularity)
            .Select(MapMovie)
            .ToArray();
    }

    public async Task<IReadOnlyList<Movie>> GetUpcomingAsync(
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var response = await tmdbClient.GetUpcomingAsync(page, cancellationToken);

        return response.Results
            .Where(movie => !movie.Adult)
            .OrderBy(movie => ParseReleaseDate(movie.ReleaseDate) ?? DateOnly.MaxValue)
            .ThenByDescending(movie => movie.Popularity)
            .Select(MapMovie)
            .ToArray();
    }

    private static Movie MapMovie(TmdbMovie movie)
    {
        var title = !string.IsNullOrWhiteSpace(movie.Title)
            ? movie.Title.Trim()
            : !string.IsNullOrWhiteSpace(movie.OriginalTitle)
                ? movie.OriginalTitle.Trim()
                : "İsimsiz Film";

        var originalTitle = string.IsNullOrWhiteSpace(movie.OriginalTitle)
            ? null
            : movie.OriginalTitle.Trim();

        var posterUrl = string.IsNullOrWhiteSpace(movie.PosterPath)
            ? null
            : $"{PosterBaseUrl}/{movie.PosterPath.TrimStart('/')}";

        return new Movie(
            movie.Id,
            title,
            originalTitle,
            movie.Overview?.Trim() ?? string.Empty,
            posterUrl,
            ParseReleaseDate(movie.ReleaseDate),
            movie.VoteAverage,
            movie.Popularity,
            $"{MovieBaseUrl}/{movie.Id}?language=tr-TR");
    }

    private static DateOnly? ParseReleaseDate(string? value) =>
        DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var releaseDate)
            ? releaseDate
            : null;
}
