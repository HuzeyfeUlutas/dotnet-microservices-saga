using MediatR;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.Common.Exceptions;
using Notification.Application.DTOs;

namespace Notification.Application.Features.Notifications.GetNotificationById;

public class GetNotificationByIdHandler(INotificationDbContext context)
    : IRequestHandler<GetNotificationByIdQuery, NotificationMessageDto>
{
    public async Task<NotificationMessageDto> Handle(GetNotificationByIdQuery request, CancellationToken cancellationToken)
    {
        var notification = await context.NotificationMessages
            .AsNoTracking()
            .Where(x => x.Id == request.Id)
            .Select(x => new NotificationMessageDto(
                x.Id,
                x.Channel,
                x.NotificationType,
                x.Recipient,
                x.Subject,
                x.Body,
                x.Status,
                x.SourceEventId,
                x.CorrelationId,
                x.ScheduledAtUtc,
                x.ProcessingStartedAtUtc,
                x.SentAtUtc,
                x.FailedAtUtc,
                x.FailureReason,
                x.CreatedAtUtc,
                x.DeliveryAttempts
                    .OrderBy(attempt => attempt.AttemptNumber)
                    .Select(attempt => new NotificationDeliveryAttemptDto(
                        attempt.Id,
                        attempt.AttemptNumber,
                        attempt.Provider,
                        attempt.Status,
                        attempt.StartedAtUtc,
                        attempt.CompletedAtUtc,
                        attempt.ProviderMessageId,
                        attempt.ErrorMessage))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException($"Notification '{request.Id}' was not found.");
        }

        return notification;
    }
}
