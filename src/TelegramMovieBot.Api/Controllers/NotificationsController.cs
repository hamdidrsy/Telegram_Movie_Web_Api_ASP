using Microsoft.AspNetCore.Mvc;
using TelegramMovieBot.Api.Models;
using TelegramMovieBot.Api.Services;

namespace TelegramMovieBot.Api.Controllers;

[ApiController]
[Route("api/notifications")]
public sealed class NotificationsController(
    IMovieNotificationService notificationService,
    IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("test")]
    [ProducesResponseType<NotificationTestResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NotificationTestResponse>> SendTestNotification(
        CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var sentMessageCount = await notificationService
            .SendMovieNotificationAsync(cancellationToken);

        return Ok(new NotificationTestResponse(
            "Test bildirimi gönderildi.",
            sentMessageCount));
    }
}
