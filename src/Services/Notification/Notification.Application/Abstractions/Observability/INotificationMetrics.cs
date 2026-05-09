namespace Notification.Application.Abstractions.Observability;

public interface INotificationMetrics
{
    void RecordNotificationCreated();
    void RecordNotificationSent();
    void RecordNotificationFailed();
    void RecordNotificationSkipped();
    void RecordNotificationCancelled();
    void RecordDeliveryAttemptStarted();
    void RecordDeliveryAttemptSucceeded();
    void RecordDeliveryAttemptFailed();
}
