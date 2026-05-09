using MediatR;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.DTOs;

namespace Notification.Application.Features.Notifications.GetNotifications;

public class GetNotificationsHandler(INotificationDbContext context)
    : IRequestHandler<GetNotificationsQuery, IReadOnlyCollection<NotificationMessageListItemDto>>
{
    public async Task<IReadOnlyCollection<NotificationMessageListItemDto>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.NotificationMessages.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Recipient))
        {
            query = query.Where(x => x.Recipient == request.Recipient.Trim());
        }

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.NotificationType))
        {
            query = query.Where(x => x.NotificationType == request.NotificationType.Trim());
        }

        var notifications = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new NotificationMessageListItemDto(
                x.Id,
                x.Channel,
                x.NotificationType,
                x.Recipient,
                x.Subject,
                x.Status,
                x.SourceEventId,
                x.CorrelationId,
                x.ScheduledAtUtc,
                x.SentAtUtc,
                x.FailedAtUtc,
                x.FailureReason,
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return notifications;
    }
}
