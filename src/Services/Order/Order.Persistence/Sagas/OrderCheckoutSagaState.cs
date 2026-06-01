using MassTransit;

namespace Order.Persistence.Sagas;

public class OrderCheckoutSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public Guid OrderId { get; set; }
    public Guid PaymentId { get; set; }
    public string CurrentState { get; set; } = null!;
    public Guid? LastProcessedEventId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public uint RowVersion { get; set; }
}
