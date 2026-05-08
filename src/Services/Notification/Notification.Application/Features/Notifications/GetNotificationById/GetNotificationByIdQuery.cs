using MediatR;
using Notification.Application.DTOs;

namespace Notification.Application.Features.Notifications.GetNotificationById;

public sealed record GetNotificationByIdQuery(Guid Id) : IRequest<NotificationMessageDto>;
