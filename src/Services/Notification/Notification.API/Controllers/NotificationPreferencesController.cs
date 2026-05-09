using MediatR;
using Microsoft.AspNetCore.Mvc;
using Notification.API.Contracts.NotificationPreferences;
using Notification.Application.Features.NotificationPreferences.GetRecipientPreferences;
using Notification.Application.Features.NotificationPreferences.SetRecipientPreference;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/notification-preferences")]
public class NotificationPreferencesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetByRecipient(
        [FromQuery] string recipientId,
        CancellationToken cancellationToken)
    {
        var preferences = await sender.Send(new GetRecipientPreferencesQuery(recipientId), cancellationToken);
        return Ok(preferences);
    }

    [HttpPut("{recipientId}")]
    public async Task<IActionResult> Set(
        string recipientId,
        [FromBody] SetRecipientPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new SetRecipientPreferenceCommand(
                recipientId,
                request.Channel,
                request.NotificationType,
                request.IsEnabled,
                request.DisabledReason),
            cancellationToken);

        return NoContent();
    }
}
