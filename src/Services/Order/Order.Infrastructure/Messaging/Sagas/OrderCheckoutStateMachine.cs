using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Order.Application.Contracts.IntegrationEvents;
using Order.Infrastructure.Messaging.Sagas.Activities;
using Order.Persistence.Sagas;
using System.Linq.Expressions;

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

        Event(() => OrderCheckoutStarted, configuration =>
        {
            configuration.CorrelateBy(
                    (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId)
                .SelectId(context => context.Message.OrderId);
            configuration.InsertOnInitial = true;
            configuration.SetSagaFactory(context => new OrderCheckoutSagaState
            {
                CorrelationId = context.CorrelationId ?? context.Message.OrderId,
                OrderId = context.Message.OrderId,
                PaymentId = context.Message.PaymentId,
                CreatedAtUtc = context.Message.OccurredAtUtc,
                UpdatedAtUtc = context.Message.OccurredAtUtc
            });
        });

        ConfigureOrderCorrelation(() => PaymentAuthorized, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => PaymentAuthorizationFailed, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => StockCommitted, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => StockCommitFailed, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => PaymentCaptured, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => PaymentCaptureFailed, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => StockReleased, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => StockReleaseFailed, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => CommittedStockReversed, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => CommittedStockReverseFailed, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => PaymentAuthorizationVoided, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => PaymentAuthorizationVoidFailed, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => PaymentCancelled, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);
        ConfigureOrderCorrelation(() => PaymentCancellationFailed, (sagaState, consumeContext) => sagaState.OrderId == consumeContext.Message.OrderId);

        Initially(
            When(OrderCheckoutStarted)
                .TransitionTo(WaitingForPayment));

        During(WaitingForPayment,
            When(PaymentAuthorized)
                .Activity(activity => activity.OfType<RequestStockCommitActivity>())
                .TransitionTo(StockCommitRequested));

        During(StockCommitRequested,
            When(StockCommitted)
                .Activity(activity => activity.OfType<RequestPaymentCaptureActivity>())
                .TransitionTo(CaptureRequested));

        During(CaptureRequested,
            When(PaymentCaptured)
                .Activity(activity => activity.OfType<ConfirmOrderActivity>())
                .TransitionTo(Completed));

        DuringAny(
            Ignore(PaymentAuthorizationFailed),
            Ignore(PaymentCaptureFailed),
            Ignore(StockCommitFailed),
            Ignore(StockReleased),
            Ignore(StockReleaseFailed),
            Ignore(CommittedStockReversed),
            Ignore(CommittedStockReverseFailed),
            Ignore(PaymentAuthorizationVoided),
            Ignore(PaymentAuthorizationVoidFailed),
            Ignore(PaymentCancelled),
            Ignore(PaymentCancellationFailed));
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

    public Event<OrderCheckoutStarted> OrderCheckoutStarted { get; private set; } = null!;
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

    private void ConfigureOrderCorrelation<TMessage>(
        Expression<Func<Event<TMessage>>> eventProperty,
        Expression<Func<OrderCheckoutSagaState, ConsumeContext<TMessage>, bool>> correlationExpression)
        where TMessage : class
    {
        Event(eventProperty, configuration =>
        {
            configuration.CorrelateBy(correlationExpression);
            configuration.OnMissingInstance(missing => missing.Discard());
        });
    }
}
