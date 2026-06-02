using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Enums;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class FailOrderAfterAuthorizationVoidActivity(
    OrderDbContext context,
    IIntegrationEventPublisher integrationEventPublisher) : IStateMachineActivity<OrderCheckoutSagaState, PaymentAuthorizationVoided>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, PaymentAuthorizationVoided> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentAuthorizationVoided> next)
    {
        var message = behaviorContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, behaviorContext.CancellationToken)
            ?? throw new InvalidOperationException($"Order '{message.OrderId}' was not found while finalizing checkout failure.");
        var failureReason = behaviorContext.Saga.FailureReason ?? "Checkout compensation completed after payment processing failure.";

        if (order.Status != OrderStatus.Failed)
        {
            order.MarkAsFailed(failureReason);
        }

        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.CompletedAtUtc = DateTime.UtcNow;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await integrationEventPublisher.PublishAsync(
            new OrderFailed(
                Guid.NewGuid(),
                order.Id,
                order.BuyerId,
                message.PaymentId,
                order.TotalAmount,
                order.Currency,
                failureReason,
                order.Lines.ToIntegrationItems(),
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, PaymentAuthorizationVoided, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentAuthorizationVoided> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("fail-order-after-authorization-void");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
