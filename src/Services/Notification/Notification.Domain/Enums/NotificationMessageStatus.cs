namespace Notification.Domain.Enums;

public enum NotificationMessageStatus
{
    Pending = 1,
    Processing = 2,
    Sent = 3,
    Failed = 4,
    Cancelled = 5,
    Skipped = 6
}
