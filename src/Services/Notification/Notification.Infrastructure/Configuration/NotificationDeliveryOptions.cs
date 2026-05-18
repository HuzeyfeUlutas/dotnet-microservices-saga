namespace Notification.Infrastructure.Configuration;

public sealed class NotificationDeliveryOptions
{
    public const string SectionName = "NotificationDelivery";

    public int DispatcherIntervalSeconds { get; init; } = 10;
    public int BatchSize { get; init; } = 20;
}
