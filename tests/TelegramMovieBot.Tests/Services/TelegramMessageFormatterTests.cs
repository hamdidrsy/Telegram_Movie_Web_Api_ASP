using TelegramMovieBot.Api.Models;
using TelegramMovieBot.Api.Options;
using TelegramMovieBot.Api.Services;

namespace TelegramMovieBot.Tests.Services;

public sealed class TelegramMessageFormatterTests
{
    [Fact]
    public void FormatNowPlaying_EscapesHtmlAndFormatsTurkishDate()
    {
        var formatter = CreateFormatter(maxMovies: 8);
        var movie = CreateMovie(
            id: 1,
            title: "A < B & C > D",
            releaseDate: new DateOnly(2026, 8, 16),
            voteAverage: 7.45);

        var message = Assert.Single(formatter.FormatNowPlaying([movie]));

        Assert.Contains("A &lt; B &amp; C &gt; D", message);
        Assert.Contains("16 Ağustos 2026", message);
        Assert.Contains("7,5/10", message);
        Assert.DoesNotContain("A < B", message);
    }

    [Fact]
    public void FormatUpcoming_WithoutDateOrVote_OmitsThoseFields()
    {
        var formatter = CreateFormatter(maxMovies: 8);
        var movie = CreateMovie(1, "Tarihsiz Film", null, voteAverage: 0);

        var message = Assert.Single(formatter.FormatUpcoming([movie]));

        Assert.DoesNotContain("📅", message);
        Assert.DoesNotContain("⭐", message);
        Assert.Contains("Film detayları", message);
    }

    [Fact]
    public void FormatNowPlaying_RespectsMaximumMovieCount()
    {
        var formatter = CreateFormatter(maxMovies: 2);
        var movies = new[]
        {
            CreateMovie(1, "Bir", null, 0),
            CreateMovie(2, "İki", null, 0),
            CreateMovie(3, "Üç", null, 0)
        };

        var message = string.Join("\n", formatter.FormatNowPlaying(movies));

        Assert.Contains("Bir", message);
        Assert.Contains("İki", message);
        Assert.DoesNotContain("Üç", message);
    }

    [Fact]
    public void FormatNowPlaying_NeverExceedsSafeMessageLength()
    {
        var formatter = CreateFormatter(maxMovies: 20);
        var movies = Enumerable.Range(1, 20)
            .Select(index => CreateMovie(
                index,
                new string('F', 500),
                new DateOnly(2026, 8, 16),
                8.2))
            .ToArray();

        var messages = formatter.FormatNowPlaying(movies);

        Assert.All(messages, message =>
            Assert.InRange(message.Length, 1, TelegramMessageFormatter.SafeMessageLength));
    }

    private static TelegramMessageFormatter CreateFormatter(int maxMovies)
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new NotificationOptions { MaxMoviesPerList = maxMovies });

        return new TelegramMessageFormatter(options);
    }

    private static Movie CreateMovie(
        int id,
        string title,
        DateOnly? releaseDate,
        double voteAverage) =>
        new(
            id,
            title,
            null,
            string.Empty,
            null,
            releaseDate,
            voteAverage,
            1,
            $"https://www.themoviedb.org/movie/{id}?language=tr-TR&source=test");
}
