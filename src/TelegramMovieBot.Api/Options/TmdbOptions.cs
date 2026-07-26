using System.ComponentModel.DataAnnotations;

namespace TelegramMovieBot.Api.Options;

public sealed class TmdbOptions
{
    public const string SectionName = "Tmdb";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = "https://api.themoviedb.org/3/";

    public string AccessToken { get; init; } = string.Empty;

    [Required]
    public string Language { get; init; } = "tr-TR";

    [Required]
    public string Region { get; init; } = "TR";
}
