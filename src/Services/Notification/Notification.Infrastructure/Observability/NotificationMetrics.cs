using System.Diagnostics.Metrics;
using Notification.Application.Abstractions.Observability;

namespace Notification.Infrastructure.Observability;

public sealed class NotificationMetrics : INotificationMetrics
{
    public const string MeterName = "MarketplaceOrderPlatform.Notification";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> NotificationCreatedCounter =
        Meter.CreateCounter<long>("notification.messages.created");
    private static readonly Counter<long> NotificationSentCounter =
        Meter.CreateCounter<long>("notification.messages.sent");
    private static readonly Counter<long> NotificationFailedCounter =
        Meter.CreateCounter<long>("notification.messages.failed");
    private static readonly Counter<long> NotificationSkippedCounter =
        Meter.CreateCounter<long>("notification.messages.skipped");
    private static readonly Counter<long> NotificationCancelledCounter =
        Meter.CreateCounter<long>("notification.messages.cancelled");
    private static readonly Counter<long> DeliveryAttemptStartedCounter =
        Meter.CreateCounter<long>("notification.delivery_attempts.started");
    private static readonly Counter<long> DeliveryAttemptSucceededCounter =
        Meter.CreateCounter<long>("notification.delivery_attempts.succeeded");
    private static readonly Counter<long> DeliveryAttemptFailedCounter =
        Meter.CreateCounter<long>("notification.delivery_attempts.failed");

    public void RecordNotificationCreated()
    {
        NotificationCreatedCounter.Add(1);
    }

    public void RecordNotificationSent()
    {
        NotificationSentCounter.Add(1);
    }

    public void RecordNotificationFailed()
    {
        NotificationFailedCounter.Add(1);
    }

    public void RecordNotificationSkipped()
    {
        NotificationSkippedCounter.Add(1);
    }

    public void RecordNotificationCancelled()
    {
        NotificationCancelledCounter.Add(1);
    }

    public void RecordDeliveryAttemptStarted()
    {
        DeliveryAttemptStartedCounter.Add(1);
    }

    public void RecordDeliveryAttemptSucceeded()
    {
        DeliveryAttemptSucceededCounter.Add(1);
    }

    public void RecordDeliveryAttemptFailed()
    {
        DeliveryAttemptFailedCounter.Add(1);
    }
}
