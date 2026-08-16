using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using TelegramMovieBot.Api.Models;
using TelegramMovieBot.Api.Options;

namespace TelegramMovieBot.Api.Services;

public sealed class TelegramMessageFormatter(
    IOptions<NotificationOptions> options)
{
    public const int SafeMessageLength = 3500;
    private const int MaximumTitleLength = 200;
    private const string Attribution =
        "ℹ️ Film verileri TMDB tarafından sağlanmaktadır.";
    private static readonly CultureInfo TurkishCulture = new("tr-TR");
    private readonly NotificationOptions _options = options.Value;

    public IReadOnlyList<string> FormatNowPlaying(IReadOnlyList<Movie> movies) =>
        Format("🎬 <b>Vizyondaki Filmler</b>", movies);

    public IReadOnlyList<string> FormatUpcoming(IReadOnlyList<Movie> movies) =>
        Format("🚀 <b>Yakında Vizyona Girecek Filmler</b>", movies);

    private IReadOnlyList<string> Format(string heading, IReadOnlyList<Movie> movies)
    {
        var selectedMovies = movies.Take(_options.MaxMoviesPerList).ToArray();

        if (selectedMovies.Length == 0)
        {
            return [$"{heading}\n\nBu listede film bulunamadı.\n\n{Attribution}"];
        }

        var messages = new List<string>();
        var builder = StartMessage(heading);

        for (var index = 0; index < selectedMovies.Length; index++)
        {
            var entry = FormatMovie(selectedMovies[index], index + 1);

            if (builder.Length + entry.Length + Attribution.Length > SafeMessageLength)
            {
                FinishMessage(builder);
                messages.Add(builder.ToString());
                builder = StartMessage(heading);
            }

            builder.Append(entry);
        }

        FinishMessage(builder);
        messages.Add(builder.ToString());

        return messages;
    }

    private static StringBuilder StartMessage(string heading) =>
        new StringBuilder(heading).AppendLine().AppendLine();

    private static void FinishMessage(StringBuilder builder) =>
        builder.AppendLine().Append(Attribution);

    private static string FormatMovie(Movie movie, int number)
    {
        var title = movie.Title.Length > MaximumTitleLength
            ? $"{movie.Title[..MaximumTitleLength]}…"
            : movie.Title;
        var safeTitle = WebUtility.HtmlEncode(title);
        var safeUrl = WebUtility.HtmlEncode(movie.TmdbUrl);

        var details = new List<string>();

        if (movie.ReleaseDate is { } releaseDate)
        {
            details.Add($"📅 {releaseDate.ToString("d MMMM yyyy", TurkishCulture)}");
        }

        if (movie.VoteAverage > 0)
        {
            details.Add($"⭐ {movie.VoteAverage.ToString("0.0", TurkishCulture)}/10");
        }

        var builder = new StringBuilder()
            .Append(number)
            .Append(". <b>")
            .Append(safeTitle)
            .AppendLine("</b>");

        if (details.Count > 0)
        {
            builder.AppendLine(string.Join(" · ", details));
        }

        builder
            .Append("🔗 <a href=\"")
            .Append(safeUrl)
            .AppendLine("\">Film detayları</a>")
            .AppendLine();

        return builder.ToString();
    }
}
