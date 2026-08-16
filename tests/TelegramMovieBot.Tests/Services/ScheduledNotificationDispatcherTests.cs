using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using TelegramMovieBot.Api.Services;

namespace TelegramMovieBot.Tests.Services;

public sealed class ScheduledNotificationDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_AfterSuccess_SkipsSameDate()
    {
        var notificationService = new StubNotificationService();
        var delay = new StubDelay();
        var dispatcher = CreateDispatcher(notificationService, delay);
        var date = new DateOnly(2026, 8, 16);

        var firstResult = await dispatcher.DispatchAsync(date, CancellationToken.None);
        var secondResult = await dispatcher.DispatchAsync(date, CancellationToken.None);

        Assert.True(firstResult);
        Assert.False(secondResult);
        Assert.Equal(1, notificationService.CallCount);
        Assert.Empty(delay.Delays);
    }

    [Fact]
    public async Task DispatchAsync_RetriesTemporaryFailuresThenSucceeds()
    {
        var notificationService = new StubNotificationService(failuresBeforeSuccess: 2);
        var delay = new StubDelay();
        var dispatcher = CreateDispatcher(notificationService, delay);

        var result = await dispatcher.DispatchAsync(
            new DateOnly(2026, 8, 16),
            CancellationToken.None);

        Assert.True(result);
        Assert.Equal(3, notificationService.CallCount);
        Assert.Equal(
            [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5)],
            delay.Delays);
    }

    [Fact]
    public async Task DispatchAsync_WhenAllAttemptsFail_DoesNotMarkDateSuccessful()
    {
        var notificationService = new StubNotificationService(failuresBeforeSuccess: 3);
        var delay = new StubDelay();
        var dispatcher = CreateDispatcher(notificationService, delay);
        var date = new DateOnly(2026, 8, 16);

        var failedResult = await dispatcher.DispatchAsync(date, CancellationToken.None);
        var nextResult = await dispatcher.DispatchAsync(date, CancellationToken.None);

        Assert.False(failedResult);
        Assert.True(nextResult);
        Assert.Equal(4, notificationService.CallCount);
    }

    [Fact]
    public async Task DispatchAsync_WhenCancelled_StopsRetrying()
    {
        var notificationService = new StubNotificationService(failuresBeforeSuccess: 5);
        var delay = new CancellingDelay();
        var dispatcher = CreateDispatcher(notificationService, delay);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            dispatcher.DispatchAsync(
                new DateOnly(2026, 8, 16),
                new CancellationToken(canceled: true)));

        Assert.Equal(1, notificationService.CallCount);
    }

    private static ScheduledNotificationDispatcher CreateDispatcher(
        IMovieNotificationService notificationService,
        INotificationDelay delay)
    {
        var services = new ServiceCollection();
        services.AddSingleton(notificationService);
        var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();

        return new ScheduledNotificationDispatcher(
            scopeFactory,
            delay,
            NullLogger<ScheduledNotificationDispatcher>.Instance);
    }

    private sealed class StubNotificationService(int failuresBeforeSuccess = 0)
        : IMovieNotificationService
    {
        public int CallCount { get; private set; }

        public Task<int> SendMovieNotificationAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            cancellationToken.ThrowIfCancellationRequested();

            if (CallCount <= failuresBeforeSuccess)
            {
                throw new HttpRequestException("Geçici test hatası");
            }

            return Task.FromResult(2);
        }
    }

    private sealed class StubDelay : INotificationDelay
    {
        public List<TimeSpan> Delays { get; } = [];

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken)
        {
            Delays.Add(delay);
            return Task.CompletedTask;
        }
    }

    private sealed class CancellingDelay : INotificationDelay
    {
        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken) =>
            Task.FromCanceled(cancellationToken);
    }
}
