using MediatR;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Abstractions.Email;
using Notification.Application.Abstractions.Observability;
using Notification.Application.Abstractions.Persistence;
using Notification.Application.Common.Exceptions;
using Notification.Domain.Enums;

namespace Notification.Application.Features.Notifications.SendNotification;

public class SendNotificationHandler(
    INotificationDbContext context,
    IEmailSender emailSender,
    INotificationMetrics metrics) : IRequestHandler<SendNotificationCommand, EmailSendResult>
{
    public async Task<EmailSendResult> Handle(SendNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await context.NotificationMessages
            .Include(x => x.DeliveryAttempts)
            .FirstOrDefaultAsync(x => x.Id == request.NotificationMessageId, cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException($"Notification '{request.NotificationMessageId}' was not found.");
        }

        if (notification.Channel != NotificationChannel.Email)
        {
            throw new ConflictException("Only email notifications can be sent by the email sender.");
        }

        if (notification.ScheduledAtUtc.HasValue && notification.ScheduledAtUtc.Value > DateTime.UtcNow)
        {
            throw new ConflictException("Scheduled notification cannot be sent before its scheduled date.");
        }

        notification.StartDeliveryAttempt(emailSender.ProviderName);
        await SaveChangesAsync(cancellationToken);
        metrics.RecordDeliveryAttemptStarted();

        var result = await SendEmailAsync(notification.Recipient, notification.Subject, notification.Body, cancellationToken);

        if (result.Succeeded)
        {
            notification.MarkAsSent(result.ProviderMessageId);
            metrics.RecordNotificationSent();
            metrics.RecordDeliveryAttemptSucceeded();
        }
        else
        {
            notification.MarkAsFailed(result.ErrorMessage ?? "Email provider returned a failure result.");
            metrics.RecordNotificationFailed();
            metrics.RecordDeliveryAttemptFailed();
        }

        await SaveChangesAsync(cancellationToken);

        return result;
    }

    private async Task<EmailSendResult> SendEmailAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        try
        {
            return await emailSender.SendAsync(recipient, subject, body, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return EmailSendResult.Failure(emailSender.ProviderName, exception.Message);
        }
    }

    private async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException($"Notification could not be updated due to a concurrency conflict. {exception.Message}");
        }
        catch (DbUpdateException exception)
        {
            throw new ConflictException($"Notification could not be updated. {exception.Message}");
        }
    }
}
