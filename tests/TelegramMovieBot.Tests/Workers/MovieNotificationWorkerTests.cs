using Microsoft.Extensions.Logging.Abstractions;
using TelegramMovieBot.Api.Options;
using TelegramMovieBot.Api.Services;
using TelegramMovieBot.Api.Workers;

namespace TelegramMovieBot.Tests.Workers;

public sealed class MovieNotificationWorkerTests
{
    [Fact]
    public async Task StartAsync_WhenDisabled_DoesNotDispatch()
    {
        var dispatcher = new StubDispatcher();
        var worker = CreateWorker(enabled: false, dispatcher);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(0, dispatcher.CallCount);
    }

    [Fact]
    public async Task StartAsync_WhenEnabled_DoesNotDispatchImmediately()
    {
        var dispatcher = new StubDispatcher();
        var worker = CreateWorker(enabled: true, dispatcher);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(0, dispatcher.CallCount);
    }

    private static MovieNotificationWorker CreateWorker(
        bool enabled,
        IScheduledNotificationDispatcher dispatcher)
    {
        var options = Microsoft.Extensions.Options.Options.Create(
            new NotificationOptions
            {
                Enabled = enabled,
                Hour = 10,
                Minute = 0,
                TimeZone = "Europe/Istanbul"
            });
        var schedule = new NotificationSchedule(options);

        return new MovieNotificationWorker(
            schedule,
            dispatcher,
            options,
            TimeProvider.System,
            NullLogger<MovieNotificationWorker>.Instance);
    }

    private sealed class StubDispatcher : IScheduledNotificationDispatcher
    {
        public int CallCount { get; private set; }

        public Task<bool> DispatchAsync(
            DateOnly notificationDate,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(true);
        }
    }
}
