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
    private static readonly TimeSpan PaymentTimeoutDelay = TimeSpan.FromMinutes(15);

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
            configuration.CorrelateById(context => context.Message.OrderId);
            configuration.InsertOnInitial = true;
            configuration.SetSagaFactory(context => new OrderCheckoutSagaState
            {
                CorrelationId = context.Message.OrderId,
                OrderId = context.Message.OrderId,
                PaymentId = context.Message.PaymentId,
                CurrentState = Initial.Name,
                CreatedAtUtc = context.Message.OccurredAtUtc,
                UpdatedAtUtc = context.Message.OccurredAtUtc
            });
        });

        Event(() => PaymentAuthorized, configuration =>
        {
            configuration.CorrelateById(context => context.Message.OrderId);
            configuration.OnMissingInstance(missing => missing.Fault());
        });

        Event(() => PaymentAuthorizationFailed, configuration =>
        {
            configuration.CorrelateById(context => context.Message.OrderId);
            configuration.OnMissingInstance(missing => missing.Fault());
        });
        ConfigureOrderIdCorrelation(() => StockCommitted, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => StockCommitFailed, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => PaymentCaptured, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => PaymentCaptureFailed, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => StockReleased, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => StockReleaseFailed, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => CommittedStockReversed, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => CommittedStockReverseFailed, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => PaymentAuthorizationVoided, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => PaymentAuthorizationVoidFailed, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => PaymentCancelled, context => context.Message.OrderId);
        ConfigureOrderIdCorrelation(() => PaymentCancellationFailed, context => context.Message.OrderId);

        Schedule(() => PaymentTimeout, sagaState => sagaState.PaymentTimeoutTokenId, configuration =>
        {
            configuration.Delay = PaymentTimeoutDelay;
            configuration.Received = received => received.CorrelateById(context => context.Message.OrderId);
        });

        Initially(
            When(OrderCheckoutStarted)
                .Schedule(
                    PaymentTimeout,
                    context => Task.FromResult(new PaymentTimeoutExpired(
                        Guid.NewGuid(),
                        context.Saga.OrderId,
                        DateTime.UtcNow)))
                .TransitionTo(WaitingForPayment));

        During(WaitingForPayment,
            When(PaymentAuthorized)
                .Activity(activity => activity.OfType<RequestStockCommitActivity>())
                .TransitionTo(StockCommitRequested),
            When(PaymentAuthorizationFailed)
                .Activity(activity => activity.OfType<RequestStockReleaseAfterPaymentFailureActivity>())
                .TransitionTo(StockReleaseRequestedAfterPaymentFailure),
            When(PaymentTimeout.Received)
                .Activity(activity => activity.OfType<RequestPendingPaymentCancellationAfterTimeoutActivity>())
                .TransitionTo(PendingPaymentCancellationRequestedAfterTimeout));

        During(StockCommitRequested,
            When(StockCommitted)
                .Activity(activity => activity.OfType<RequestPaymentCaptureActivity>())
                .TransitionTo(CaptureRequested),
            When(StockCommitFailed)
                .Activity(activity => activity.OfType<RequestStockReleaseAfterStockCommitFailureActivity>())
                .TransitionTo(StockReleaseRequestedAfterStockCommitFailure));

        During(StockReleaseRequestedAfterPaymentFailure,
            When(StockReleased)
                .Activity(activity => activity.OfType<FailOrderPaymentAfterStockReleaseActivity>())
                .TransitionTo(PaymentFailed),
            When(StockReleaseFailed)
                .Then(context => MoveToManualReview(context.Saga, context.Message.EventId, context.Message.FailureReason))
                .TransitionTo(ManualReviewRequired));

        During(StockReleaseRequestedAfterStockCommitFailure,
            When(StockReleased)
                .Activity(activity => activity.OfType<RequestAuthorizationVoidAfterStockReleaseActivity>())
                .TransitionTo(AuthorizationVoidRequestedAfterStockCommitFailure),
            When(StockReleaseFailed)
                .Then(context => MoveToManualReview(context.Saga, context.Message.EventId, context.Message.FailureReason))
                .TransitionTo(ManualReviewRequired));

        During(AuthorizationVoidRequestedAfterStockCommitFailure,
            When(PaymentAuthorizationVoided)
                .Activity(activity => activity.OfType<FailOrderAfterAuthorizationVoidActivity>())
                .TransitionTo(Failed),
            When(PaymentAuthorizationVoidFailed)
                .Then(context => MoveToManualReview(context.Saga, context.Message.EventId, context.Message.FailureReason))
                .TransitionTo(ManualReviewRequired));

        During(PendingPaymentCancellationRequestedAfterTimeout,
            When(PaymentCancelled)
                .Activity(activity => activity.OfType<RequestStockReleaseAfterPaymentCancellationActivity>())
                .TransitionTo(StockReleaseRequestedAfterPaymentTimeout),
            When(PaymentCancellationFailed)
                .Then(context => MoveToManualReview(context.Saga, context.Message.EventId, context.Message.FailureReason))
                .TransitionTo(ManualReviewRequired));

        During(StockReleaseRequestedAfterPaymentTimeout,
            When(StockReleased)
                .Activity(activity => activity.OfType<FailOrderPaymentAfterStockReleaseActivity>())
                .TransitionTo(PaymentFailed),
            When(StockReleaseFailed)
                .Then(context => MoveToManualReview(context.Saga, context.Message.EventId, context.Message.FailureReason))
                .TransitionTo(ManualReviewRequired));

        During(CaptureRequested,
            When(PaymentCaptured)
                .Activity(activity => activity.OfType<ConfirmOrderActivity>())
                .TransitionTo(Completed),
            When(PaymentCaptureFailed)
                .Activity(activity => activity.OfType<RequestCommittedStockReverseAfterPaymentCaptureFailureActivity>())
                .TransitionTo(StockReverseRequestedAfterPaymentCaptureFailure));

        During(StockReverseRequestedAfterPaymentCaptureFailure,
            When(CommittedStockReversed)
                .Activity(activity => activity.OfType<RequestAuthorizationVoidAfterCommittedStockReverseActivity>())
                .TransitionTo(AuthorizationVoidRequestedAfterPaymentCaptureFailure),
            When(CommittedStockReverseFailed)
                .Then(context => MoveToManualReview(context.Saga, context.Message.EventId, context.Message.FailureReason))
                .TransitionTo(ManualReviewRequired));

        During(AuthorizationVoidRequestedAfterPaymentCaptureFailure,
            When(PaymentAuthorizationVoided)
                .Activity(activity => activity.OfType<FailOrderAfterAuthorizationVoidActivity>())
                .TransitionTo(Failed),
            When(PaymentAuthorizationVoidFailed)
                .Then(context => MoveToManualReview(context.Saga, context.Message.EventId, context.Message.FailureReason))
                .TransitionTo(ManualReviewRequired));

        IgnoreDuplicateAndOutOfOrderEvents();
        IgnoreFinalStateEvents();
        IgnorePaymentTimeoutIn(
            StockCommitRequested,
            StockReleaseRequestedAfterPaymentFailure,
            StockReleaseRequestedAfterStockCommitFailure,
            StockReverseRequestedAfterPaymentCaptureFailure,
            AuthorizationVoidRequestedAfterStockCommitFailure,
            AuthorizationVoidRequestedAfterPaymentCaptureFailure,
            PendingPaymentCancellationRequestedAfterTimeout,
            StockReleaseRequestedAfterPaymentTimeout,
            CaptureRequested,
            PaymentFailed,
            Completed,
            Failed,
            ManualReviewRequired);
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
    public Schedule<OrderCheckoutSagaState, PaymentTimeoutExpired> PaymentTimeout { get; private set; } = null!;

    private void ConfigureOrderIdCorrelation<TMessage>(
        Expression<Func<Event<TMessage>>> eventProperty,
        Func<ConsumeContext<TMessage>, Guid> orderIdSelector)
        where TMessage : class
    {
        Event(eventProperty, configuration =>
        {
            configuration.CorrelateById(orderIdSelector);
            configuration.OnMissingInstance(missing => missing.Discard());
        });
    }

    private void IgnorePaymentTimeoutIn(params State[] states)
    {
        foreach (var state in states)
        {
            During(state, Ignore(PaymentTimeout.Received));
        }
    }

    private void IgnoreDuplicateAndOutOfOrderEvents()
    {
        During(WaitingForPayment,
            Ignore(OrderCheckoutStarted),
            Ignore(StockCommitted),
            Ignore(StockCommitFailed),
            Ignore(PaymentCaptured),
            Ignore(PaymentCaptureFailed),
            Ignore(StockReleased),
            Ignore(StockReleaseFailed),
            Ignore(CommittedStockReversed),
            Ignore(CommittedStockReverseFailed),
            Ignore(PaymentAuthorizationVoided),
            Ignore(PaymentAuthorizationVoidFailed),
            Ignore(PaymentCancelled),
            Ignore(PaymentCancellationFailed));

        During(StockCommitRequested,
            Ignore(OrderCheckoutStarted),
            Ignore(PaymentAuthorized),
            Ignore(PaymentAuthorizationFailed));

        During(CaptureRequested,
            Ignore(OrderCheckoutStarted),
            Ignore(PaymentAuthorized),
            Ignore(PaymentAuthorizationFailed),
            Ignore(StockCommitted),
            Ignore(StockCommitFailed));

        During(StockReleaseRequestedAfterPaymentFailure,
            Ignore(OrderCheckoutStarted),
            Ignore(PaymentAuthorizationFailed));

        During(StockReleaseRequestedAfterStockCommitFailure,
            Ignore(OrderCheckoutStarted),
            Ignore(StockCommitFailed));

        During(StockReverseRequestedAfterPaymentCaptureFailure,
            Ignore(OrderCheckoutStarted),
            Ignore(PaymentCaptureFailed));

        During(AuthorizationVoidRequestedAfterStockCommitFailure,
            Ignore(OrderCheckoutStarted),
            Ignore(StockReleased));

        During(AuthorizationVoidRequestedAfterPaymentCaptureFailure,
            Ignore(OrderCheckoutStarted),
            Ignore(CommittedStockReversed));

        During(PendingPaymentCancellationRequestedAfterTimeout,
            Ignore(OrderCheckoutStarted));

        During(StockReleaseRequestedAfterPaymentTimeout,
            Ignore(OrderCheckoutStarted),
            Ignore(PaymentCancelled));
    }

    private void IgnoreFinalStateEvents()
    {
        IgnoreCheckoutEventsIn(PaymentFailed, Completed, Failed, ManualReviewRequired);
    }

    private void IgnoreCheckoutEventsIn(params State[] states)
    {
        foreach (var state in states)
        {
            During(state,
                Ignore(OrderCheckoutStarted),
                Ignore(PaymentAuthorized),
                Ignore(PaymentAuthorizationFailed),
                Ignore(StockCommitted),
                Ignore(StockCommitFailed),
                Ignore(PaymentCaptured),
                Ignore(PaymentCaptureFailed),
                Ignore(StockReleased),
                Ignore(StockReleaseFailed),
                Ignore(CommittedStockReversed),
                Ignore(CommittedStockReverseFailed),
                Ignore(PaymentAuthorizationVoided),
                Ignore(PaymentAuthorizationVoidFailed),
                Ignore(PaymentCancelled),
                Ignore(PaymentCancellationFailed));
        }
    }

    private static void MoveToManualReview(
        OrderCheckoutSagaState sagaState,
        Guid eventId,
        string failureReason)
    {
        sagaState.LastProcessedEventId = eventId;
        sagaState.FailureReason = failureReason;
        sagaState.UpdatedAtUtc = DateTime.UtcNow;
    }
}
