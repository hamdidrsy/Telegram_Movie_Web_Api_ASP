using System.ComponentModel.DataAnnotations;

namespace TelegramMovieBot.Api.Options;

public sealed class NotificationOptions
{
    public const string SectionName = "Notification";

    public bool Enabled { get; init; }

    [Range(0, 23)]
    public int Hour { get; init; } = 10;

    [Range(0, 59)]
    public int Minute { get; init; }

    [Required]
    public string TimeZone { get; init; } = "Europe/Istanbul";

    [Range(1, 20)]
    public int MaxMoviesPerList { get; init; } = 8;
}
