namespace TelegramMovieBot.Api.Services;

public interface IMovieNotificationService
{
    Task<int> SendMovieNotificationAsync(
        CancellationToken cancellationToken = default);
}
