using Marketplace.Contracts.Payment.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.Enums;
using Order.Persistence.Context;
using Order.Persistence.Sagas;

namespace Order.Infrastructure.Messaging.Sagas.Activities;

public sealed class ConfirmOrderActivity(
    OrderDbContext context,
    IIntegrationEventPublisher integrationEventPublisher) : IStateMachineActivity<OrderCheckoutSagaState, PaymentCaptured>
{
    public async Task Execute(
        BehaviorContext<OrderCheckoutSagaState, PaymentCaptured> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentCaptured> next)
    {
        var message = behaviorContext.Message;
        var order = await context.Orders
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == message.OrderId, behaviorContext.CancellationToken)
            ?? throw new InvalidOperationException($"Order '{message.OrderId}' was not found while confirming checkout.");

        if (order.Status == OrderStatus.WaitingForPayment)
        {
            order.MarkAsConfirmed();
        }
        else if (order.Status != OrderStatus.Confirmed)
        {
            throw new InvalidOperationException($"Order '{order.Id}' cannot be confirmed from status '{order.Status}'.");
        }

        behaviorContext.Saga.PaymentId = message.PaymentId;
        behaviorContext.Saga.LastProcessedEventId = message.EventId;
        behaviorContext.Saga.CompletedAtUtc = DateTime.UtcNow;
        behaviorContext.Saga.FailureReason = null;
        behaviorContext.Saga.UpdatedAtUtc = DateTime.UtcNow;

        await integrationEventPublisher.PublishAsync(
            new OrderConfirmed(
                Guid.NewGuid(),
                order.Id,
                order.BuyerId,
                message.PaymentId,
                order.TotalAmount,
                order.Currency,
                order.Lines.ToIntegrationItems(),
                DateTime.UtcNow),
            behaviorContext.CancellationToken);

        await next.Execute(behaviorContext);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<OrderCheckoutSagaState, PaymentCaptured, TException> behaviorContext,
        IBehavior<OrderCheckoutSagaState, PaymentCaptured> next)
        where TException : Exception
    {
        return next.Faulted(behaviorContext);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("confirm-order");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
