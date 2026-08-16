using TelegramMovieBot.Api.Clients;

namespace TelegramMovieBot.Api.Services;

public sealed class MovieNotificationService(
    MovieService movieService,
    TelegramMessageFormatter messageFormatter,
    TelegramClient telegramClient,
    ILogger<MovieNotificationService> logger) : IMovieNotificationService
{
    public async Task<int> SendMovieNotificationAsync(
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Film bildirimi hazırlanmaya başlandı.");

        var nowPlayingTask = movieService.GetNowPlayingAsync(
            page: 1,
            cancellationToken);
        var upcomingTask = movieService.GetUpcomingAsync(
            page: 1,
            cancellationToken);

        await Task.WhenAll(nowPlayingTask, upcomingTask);

        var messages = messageFormatter
            .FormatNowPlaying(await nowPlayingTask)
            .Concat(messageFormatter.FormatUpcoming(await upcomingTask))
            .ToArray();

        foreach (var message in messages)
        {
            await telegramClient.SendMessageAsync(message, cancellationToken);
        }

        logger.LogInformation(
            "Film bildirimi tamamlandı. Gönderilen mesaj sayısı: {MessageCount}",
            messages.Length);

        return messages.Length;
    }
}
