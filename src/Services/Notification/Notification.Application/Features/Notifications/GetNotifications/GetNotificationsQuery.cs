using MediatR;
using Notification.Application.DTOs;
using Notification.Domain.Enums;

namespace Notification.Application.Features.Notifications.GetNotifications;

public sealed record GetNotificationsQuery(
    string? Recipient = null,
    NotificationMessageStatus? Status = null,
    string? NotificationType = null) : IRequest<IReadOnlyCollection<NotificationMessageListItemDto>>;
