namespace TelegramMovieBot.Api.Models;

public sealed record Movie(
    int Id,
    string Title,
    string? OriginalTitle,
    string Overview,
    string? PosterUrl,
    DateOnly? ReleaseDate,
    double VoteAverage,
    double Popularity,
    string TmdbUrl);
