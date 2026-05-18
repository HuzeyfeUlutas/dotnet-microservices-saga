using MediatR;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Observability;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.Common.Exceptions;
using Notification.Domain.Entities;
using Notification.Domain.Enums;

namespace Notification.Application.Features.Notifications.CreateNotification;

public class CreateNotificationHandler(
    INotificationDbContext context,
    ICorrelationContextAccessor correlationContextAccessor,
    INotificationMetrics metrics) : IRequestHandler<CreateNotificationCommand, Guid>
{
    public async Task<Guid> Handle(CreateNotificationCommand request, CancellationToken cancellationToken)
    {
        if (request.SourceEventId.HasValue)
        {
            var existingNotificationId = await context.NotificationMessages
                .Where(x => x.SourceEventId == request.SourceEventId &&
                            x.NotificationType == request.NotificationType &&
                            x.RecipientId == request.RecipientId)
                .Select(x => (Guid?)x.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingNotificationId.HasValue)
            {
                return existingNotificationId.Value;
            }
        }

        var correlationId = request.CorrelationId ?? correlationContextAccessor.CorrelationId;
        var preference = await context.RecipientPreferences
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.RecipientId == request.RecipientId &&
                     x.Channel == NotificationChannel.Email &&
                     x.NotificationType == request.NotificationType,
                cancellationToken);

        var notification = new NotificationMessage(
            NotificationChannel.Email,
            request.NotificationType,
            request.RecipientId,
            request.Recipient,
            request.Subject,
            request.Body,
            request.SourceEventId,
            correlationId,
            request.ScheduledAtUtc);

        if (preference is not null && !preference.IsEnabled)
        {
            notification.Skip(preference.DisabledReason ?? "Recipient preference disabled.");
        }

        context.NotificationMessages.Add(notification);
        var savedNotificationId = await SaveChangesAsync(notification, cancellationToken);
        metrics.RecordNotificationCreated();

        if (notification.Status == NotificationMessageStatus.Skipped)
        {
            metrics.RecordNotificationSkipped();
        }

        return savedNotificationId;
    }

    private async Task<Guid> SaveChangesAsync(NotificationMessage notification, CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return notification.Id;
        }
        catch (DbUpdateException exception)
        {
            if (notification.SourceEventId.HasValue)
            {
                var existingNotificationId = await context.NotificationMessages
                    .AsNoTracking()
                    .Where(x => x.SourceEventId == notification.SourceEventId &&
                                x.NotificationType == notification.NotificationType &&
                                x.RecipientId == notification.RecipientId)
                    .Select(x => (Guid?)x.Id)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingNotificationId.HasValue)
                {
                    return existingNotificationId.Value;
                }
            }

            throw new ConflictException($"Notification could not be created. {exception.Message}");
        }
    }
}
