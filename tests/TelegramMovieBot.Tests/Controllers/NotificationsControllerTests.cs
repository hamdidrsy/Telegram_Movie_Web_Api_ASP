using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TelegramMovieBot.Api.Controllers;
using TelegramMovieBot.Api.Models;
using TelegramMovieBot.Api.Services;

namespace TelegramMovieBot.Tests.Controllers;

public sealed class NotificationsControllerTests
{
    [Fact]
    public async Task SendTestNotification_InDevelopment_SendsNotification()
    {
        var service = new StubNotificationService(messageCount: 2);
        var controller = new NotificationsController(
            service,
            CreateEnvironment(Environments.Development));

        var actionResult = await controller.SendTestNotification();

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<NotificationTestResponse>(okResult.Value);
        Assert.Equal(2, response.SentMessageCount);
        Assert.Equal(1, service.CallCount);
    }

    [Fact]
    public async Task SendTestNotification_InProduction_ReturnsNotFoundWithoutSending()
    {
        var service = new StubNotificationService(messageCount: 2);
        var controller = new NotificationsController(
            service,
            CreateEnvironment(Environments.Production));

        var actionResult = await controller.SendTestNotification();

        Assert.IsType<NotFoundResult>(actionResult.Result);
        Assert.Equal(0, service.CallCount);
    }

    private static IWebHostEnvironment CreateEnvironment(string environmentName) =>
        new TestWebHostEnvironment { EnvironmentName = environmentName };

    private sealed class StubNotificationService(int messageCount)
        : IMovieNotificationService
    {
        public int CallCount { get; private set; }

        public Task<int> SendMovieNotificationAsync(
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(messageCount);
        }
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "TelegramMovieBot.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
