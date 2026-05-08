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
            var alreadyExists = await context.NotificationMessages.AnyAsync(
                x => x.SourceEventId == request.SourceEventId &&
                     x.NotificationType == request.NotificationType &&
                     x.Recipient == request.Recipient,
                cancellationToken);

            if (alreadyExists)
            {
                throw new ConflictException(
                    $"Notification for event '{request.SourceEventId}' and recipient '{request.Recipient}' already exists.");
            }
        }

        var correlationId = request.CorrelationId ?? correlationContextAccessor.CorrelationId;
        var notification = new NotificationMessage(
            NotificationChannel.Email,
            request.NotificationType,
            request.Recipient,
            request.Subject,
            request.Body,
            request.SourceEventId,
            correlationId,
            request.ScheduledAtUtc);

        context.NotificationMessages.Add(notification);
        await SaveChangesAsync(cancellationToken);
        metrics.RecordNotificationCreated();

        return notification.Id;
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException($"Notification could not be created. {exception.Message}");
        }
    }
}
