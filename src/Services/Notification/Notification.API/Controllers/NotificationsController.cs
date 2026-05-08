using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Features.Notifications.GetNotificationById;
using Notification.Application.Features.Notifications.GetNotifications;
using Notification.Application.Features.Notifications.SendNotification;
using Notification.Domain.Enums;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]
public class NotificationsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? recipient,
        [FromQuery] NotificationMessageStatus? status,
        [FromQuery] string? notificationType,
        CancellationToken cancellationToken)
    {
        var notifications = await sender.Send(
            new GetNotificationsQuery(recipient, status, notificationType),
            cancellationToken);

        return Ok(notifications);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var notification = await sender.Send(new GetNotificationByIdQuery(id), cancellationToken);
        return Ok(notification);
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<IActionResult> Retry(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SendNotificationCommand(id), cancellationToken);
        return Ok(result);
    }
}
