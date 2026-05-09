using MediatR;
using Notification.Application.Abstractions.Email;

namespace Notification.Application.Features.Notifications.SendNotification;

public sealed record SendNotificationCommand(Guid NotificationMessageId) : IRequest<EmailSendResult>;
