using FluentAssertions;
using MassTransit;
using Order.Infrastructure.Messaging.Sagas;
using Order.Persistence.Sagas;
using Xunit;

namespace Order.Infrastructure.Tests.Messaging.Sagas;

public class OrderCheckoutStateMachineTests
{
    [Fact]
    public void Saga_state_should_implement_mass_transit_state_machine_contract()
    {
        typeof(OrderCheckoutSagaState).Should().Implement<SagaStateMachineInstance>();
    }

    [Fact]
    public void State_machine_should_define_existing_checkout_workflow_states()
    {
        var stateMachine = new OrderCheckoutStateMachine();

        stateMachine.States.Select(x => x.Name).Should().Contain(
        [
            OrderCheckoutSagaStatus.WaitingForPayment,
            OrderCheckoutSagaStatus.StockCommitRequested,
            OrderCheckoutSagaStatus.StockReleaseRequestedAfterPaymentFailure,
            OrderCheckoutSagaStatus.StockReleaseRequestedAfterStockCommitFailure,
            OrderCheckoutSagaStatus.StockReverseRequestedAfterPaymentCaptureFailure,
            OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterStockCommitFailure,
            OrderCheckoutSagaStatus.AuthorizationVoidRequestedAfterPaymentCaptureFailure,
            OrderCheckoutSagaStatus.PendingPaymentCancellationRequestedAfterTimeout,
            OrderCheckoutSagaStatus.StockReleaseRequestedAfterPaymentTimeout,
            OrderCheckoutSagaStatus.CaptureRequested,
            OrderCheckoutSagaStatus.PaymentFailed,
            OrderCheckoutSagaStatus.Completed,
            OrderCheckoutSagaStatus.Failed,
            OrderCheckoutSagaStatus.ManualReviewRequired
        ]);
    }

    [Fact]
    public void State_machine_should_define_existing_checkout_result_events()
    {
        var stateMachine = new OrderCheckoutStateMachine();

        stateMachine.Events.Should().HaveCount(15);
    }
}
