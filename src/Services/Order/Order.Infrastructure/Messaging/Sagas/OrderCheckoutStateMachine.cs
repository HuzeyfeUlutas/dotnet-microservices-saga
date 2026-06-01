using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas;

public sealed class OrderCheckoutStateMachine : MassTransitStateMachine<OrderCheckoutSagaState>
{
    public OrderCheckoutStateMachine()
    {
        InstanceState(x => x.CurrentState);

        State(() => WaitingForPayment);
        State(() => StockCommitRequested);
        State(() => StockReleaseRequestedAfterPaymentFailure);
        State(() => StockReleaseRequestedAfterStockCommitFailure);
        State(() => StockReverseRequestedAfterPaymentCaptureFailure);
        State(() => AuthorizationVoidRequestedAfterStockCommitFailure);
        State(() => AuthorizationVoidRequestedAfterPaymentCaptureFailure);
        State(() => PendingPaymentCancellationRequestedAfterTimeout);
        State(() => StockReleaseRequestedAfterPaymentTimeout);
        State(() => CaptureRequested);
        State(() => PaymentFailed);
        State(() => Completed);
        State(() => Failed);
        State(() => ManualReviewRequired);

        ConfigureOrderCorrelation(() => PaymentAuthorized);
        ConfigureOrderCorrelation(() => PaymentAuthorizationFailed);
        ConfigureOrderCorrelation(() => StockCommitted);
        ConfigureOrderCorrelation(() => StockCommitFailed);
        ConfigureOrderCorrelation(() => PaymentCaptured);
        ConfigureOrderCorrelation(() => PaymentCaptureFailed);
        ConfigureOrderCorrelation(() => StockReleased);
        ConfigureOrderCorrelation(() => StockReleaseFailed);
        ConfigureOrderCorrelation(() => CommittedStockReversed);
        ConfigureOrderCorrelation(() => CommittedStockReverseFailed);
        ConfigureOrderCorrelation(() => PaymentAuthorizationVoided);
        ConfigureOrderCorrelation(() => PaymentAuthorizationVoidFailed);
        ConfigureOrderCorrelation(() => PaymentCancelled);
        ConfigureOrderCorrelation(() => PaymentCancellationFailed);
    }

    public State WaitingForPayment { get; private set; } = null!;
    public State StockCommitRequested { get; private set; } = null!;
    public State StockReleaseRequestedAfterPaymentFailure { get; private set; } = null!;
    public State StockReleaseRequestedAfterStockCommitFailure { get; private set; } = null!;
    public State StockReverseRequestedAfterPaymentCaptureFailure { get; private set; } = null!;
    public State AuthorizationVoidRequestedAfterStockCommitFailure { get; private set; } = null!;
    public State AuthorizationVoidRequestedAfterPaymentCaptureFailure { get; private set; } = null!;
    public State PendingPaymentCancellationRequestedAfterTimeout { get; private set; } = null!;
    public State StockReleaseRequestedAfterPaymentTimeout { get; private set; } = null!;
    public State CaptureRequested { get; private set; } = null!;
    public State PaymentFailed { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;
    public State ManualReviewRequired { get; private set; } = null!;

    public Event<PaymentAuthorized> PaymentAuthorized { get; private set; } = null!;
    public Event<PaymentAuthorizationFailed> PaymentAuthorizationFailed { get; private set; } = null!;
    public Event<StockCommitted> StockCommitted { get; private set; } = null!;
    public Event<StockCommitFailed> StockCommitFailed { get; private set; } = null!;
    public Event<PaymentCaptured> PaymentCaptured { get; private set; } = null!;
    public Event<PaymentCaptureFailed> PaymentCaptureFailed { get; private set; } = null!;
    public Event<StockReleased> StockReleased { get; private set; } = null!;
    public Event<StockReleaseFailed> StockReleaseFailed { get; private set; } = null!;
    public Event<CommittedStockReversed> CommittedStockReversed { get; private set; } = null!;
    public Event<CommittedStockReverseFailed> CommittedStockReverseFailed { get; private set; } = null!;
    public Event<PaymentAuthorizationVoided> PaymentAuthorizationVoided { get; private set; } = null!;
    public Event<PaymentAuthorizationVoidFailed> PaymentAuthorizationVoidFailed { get; private set; } = null!;
    public Event<PaymentCancelled> PaymentCancelled { get; private set; } = null!;
    public Event<PaymentCancellationFailed> PaymentCancellationFailed { get; private set; } = null!;

    private void ConfigureOrderCorrelation<TMessage>(System.Linq.Expressions.Expression<Func<Event<TMessage>>> eventProperty)
        where TMessage : class
    {
        Event(eventProperty, configuration =>
        {
            configuration.CorrelateBy(
                (sagaState, consumeContext) => sagaState.OrderId == GetOrderId(consumeContext.Message));
            configuration.OnMissingInstance(missing => missing.Discard());
        });
    }

    private static Guid GetOrderId<TMessage>(TMessage message)
        where TMessage : class
    {
        return message switch
        {
            PaymentAuthorized value => value.OrderId,
            PaymentAuthorizationFailed value => value.OrderId,
            StockCommitted value => value.OrderId,
            StockCommitFailed value => value.OrderId,
            PaymentCaptured value => value.OrderId,
            PaymentCaptureFailed value => value.OrderId,
            StockReleased value => value.OrderId,
            StockReleaseFailed value => value.OrderId,
            CommittedStockReversed value => value.OrderId,
            CommittedStockReverseFailed value => value.OrderId,
            PaymentAuthorizationVoided value => value.OrderId,
            PaymentAuthorizationVoidFailed value => value.OrderId,
            PaymentCancelled value => value.OrderId,
            PaymentCancellationFailed value => value.OrderId,
            _ => throw new ArgumentOutOfRangeException(nameof(message), message.GetType().FullName, "Unsupported checkout saga message type.")
        };
    }
}
