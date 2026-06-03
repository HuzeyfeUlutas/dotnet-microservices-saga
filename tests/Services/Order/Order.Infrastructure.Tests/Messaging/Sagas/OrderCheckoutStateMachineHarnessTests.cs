using FluentAssertions;
using Marketplace.Contracts.Inventory.V1;
using Marketplace.Contracts.Payment.V1;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Order.Application.Abstractions.Messaging;
using Order.Application.Contracts.IntegrationEvents;
using Order.Domain.ValueObjects;
using Order.Infrastructure.Messaging.Sagas;
using Order.Infrastructure.Messaging.Sagas.Activities;
using Order.Persistence.Context;
using Order.Persistence.Sagas;
using Xunit;
using Xunit.Sdk;

namespace Order.Infrastructure.Tests.Messaging.Sagas;

public class OrderCheckoutStateMachineHarnessTests
{
    [Fact]
    public async Task Successful_checkout_should_reach_completed_state()
    {
        var order = CreateOrder("idem-harness-success");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));

        if (!await harness.Published.Any<CommitStockRequested>(x => x.Context.Message.OrderId == order.Id))
        {
            throw new XunitException(GetFaultMessage<PaymentAuthorized>(harness));
        }

        await harness.Bus.Publish(new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));

        (await harness.Consumed.Any<StockCommitted>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();
        if (!await harness.Published.Any<CapturePaymentRequested>(x => x.Context.Message.OrderId == order.Id))
        {
            throw new XunitException(GetFaultMessage<StockCommitted>(harness));
        }

        await harness.Bus.Publish(new PaymentCaptured(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.Completed)).Should().NotBeNull();
        await integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderConfirmed>(message =>
                message.OrderId == order.Id &&
                message.PaymentId == paymentId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Payment_authorization_failure_should_release_stock_and_reach_payment_failed_state()
    {
        var order = CreateOrder("idem-harness-payment-failed");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await harness.Bus.Publish(new PaymentAuthorizationFailed(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            "3ds failed",
            DateTime.UtcNow));

        (await harness.Published.Any<ReleaseStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new StockReleased(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.PaymentFailed)).Should().NotBeNull();
        await integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderPaymentFailed>(message =>
                message.OrderId == order.Id &&
                message.PaymentId == paymentId &&
                message.FailureReason == "3ds failed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Early_payment_authorized_event_should_be_retried_until_checkout_saga_exists()
    {
        var order = CreateOrder("idem-harness-early-payment-authorized");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));

        await harness.Bus.Publish(new OrderCheckoutStarted(Guid.NewGuid(), order.Id, paymentId, DateTime.UtcNow));

        (await harness.Published.Any<CommitStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();
        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.StockCommitRequested)).Should().NotBeNull();

        await harness.Bus.Publish(new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        (await harness.Published.Any<CapturePaymentRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();
        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.CaptureRequested)).Should().NotBeNull();
    }

    [Fact]
    public async Task Early_payment_authorization_failure_event_should_be_retried_until_checkout_saga_exists()
    {
        var order = CreateOrder("idem-harness-early-payment-failed");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await harness.Bus.Publish(new PaymentAuthorizationFailed(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            "3ds failed",
            DateTime.UtcNow));

        await harness.Bus.Publish(new OrderCheckoutStarted(Guid.NewGuid(), order.Id, paymentId, DateTime.UtcNow));

        (await harness.Published.Any<ReleaseStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();
        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.StockReleaseRequestedAfterPaymentFailure)).Should().NotBeNull();

        await harness.Bus.Publish(new StockReleased(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.PaymentFailed)).Should().NotBeNull();
        await integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderPaymentFailed>(message =>
                message.OrderId == order.Id &&
                message.PaymentId == paymentId &&
                message.FailureReason == "3ds failed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capture_failure_should_reverse_stock_void_authorization_and_reach_failed_state()
    {
        var order = CreateOrder("idem-harness-capture-failed");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));
        (await harness.Published.Any<CommitStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        (await harness.Published.Any<CapturePaymentRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new PaymentCaptureFailed(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            "capture failed",
            DateTime.UtcNow));
        (await harness.Published.Any<ReverseCommittedStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new CommittedStockReversed(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        (await harness.Published.Any<VoidPaymentAuthorizationRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new PaymentAuthorizationVoided(
            Guid.NewGuid(),
            Guid.NewGuid(),
            paymentId,
            order.Id,
            DateTime.UtcNow));

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.Failed)).Should().NotBeNull();
        await integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderFailed>(message =>
                message.OrderId == order.Id &&
                message.PaymentId == paymentId &&
                message.FailureReason == "capture failed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_compensation_should_reach_manual_review_state()
    {
        var order = CreateOrder("idem-harness-manual-review");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await harness.Bus.Publish(new PaymentAuthorizationFailed(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            "3ds failed",
            DateTime.UtcNow));
        (await harness.Published.Any<ReleaseStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        var stockReleaseFailed = new StockReleaseFailed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            order.Id,
            "release failed",
            DateTime.UtcNow);
        await harness.Bus.Publish(stockReleaseFailed);

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.ManualReviewRequired)).Should().NotBeNull();
        var sagaState = sagaHarness.Sagas.ContainsInState(
            order.Id,
            sagaHarness.StateMachine,
            sagaHarness.StateMachine.ManualReviewRequired);

        sagaState.Should().NotBeNull();
        sagaState!.FailureReason.Should().Be("release failed");
        sagaState.LastProcessedEventId.Should().Be(stockReleaseFailed.EventId);
        await integrationEventPublisher.DidNotReceive()
            .PublishAsync(Arg.Any<OrderFailed>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Payment_timeout_should_cancel_payment_release_stock_and_reach_payment_failed_state()
    {
        var order = CreateOrder("idem-harness-timeout");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await harness.Bus.Publish(new PaymentTimeoutExpired(Guid.NewGuid(), order.Id, DateTime.UtcNow));

        (await harness.Published.Any<CancelPendingPaymentRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new PaymentCancelled(Guid.NewGuid(), Guid.NewGuid(), paymentId, order.Id, DateTime.UtcNow));

        (await harness.Published.Any<ReleaseStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new StockReleased(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.PaymentFailed)).Should().NotBeNull();
        await integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderPaymentFailed>(message =>
                message.OrderId == order.Id &&
                message.PaymentId == paymentId &&
                message.FailureReason == "Payment timeout expired."),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stock_commit_failure_should_release_stock_void_authorization_and_reach_failed_state()
    {
        var order = CreateOrder("idem-harness-stock-commit-failed");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));
        (await harness.Published.Any<CommitStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new StockCommitFailed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            order.Id,
            "commit failed",
            DateTime.UtcNow));
        (await harness.Published.Any<ReleaseStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new StockReleased(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        (await harness.Published.Any<VoidPaymentAuthorizationRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new PaymentAuthorizationVoided(
            Guid.NewGuid(),
            Guid.NewGuid(),
            paymentId,
            order.Id,
            DateTime.UtcNow));

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.Failed)).Should().NotBeNull();
        await integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderFailed>(message =>
                message.OrderId == order.Id &&
                message.PaymentId == paymentId &&
                message.FailureReason == "commit failed"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Stock_reverse_failure_should_reach_manual_review_state()
    {
        var order = CreateOrder("idem-harness-stock-reverse-failed");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await ReachStockReverseRequestedAfterCaptureFailureAsync(harness, order, paymentId);

        var reverseFailed = new CommittedStockReverseFailed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            order.Id,
            "reverse failed",
            DateTime.UtcNow);
        await harness.Bus.Publish(reverseFailed);

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.ManualReviewRequired)).Should().NotBeNull();
        var sagaState = sagaHarness.Sagas.ContainsInState(
            order.Id,
            sagaHarness.StateMachine,
            sagaHarness.StateMachine.ManualReviewRequired);
        sagaState.Should().NotBeNull();
        sagaState!.FailureReason.Should().Be("reverse failed");
        sagaState.LastProcessedEventId.Should().Be(reverseFailed.EventId);
        await integrationEventPublisher.DidNotReceive()
            .PublishAsync(Arg.Any<OrderFailed>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Authorization_void_failure_after_capture_failure_should_reach_manual_review_state()
    {
        var order = CreateOrder("idem-harness-auth-void-failed-after-capture");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await ReachStockReverseRequestedAfterCaptureFailureAsync(harness, order, paymentId);
        await harness.Bus.Publish(new CommittedStockReversed(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        (await harness.Published.Any<VoidPaymentAuthorizationRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        var authorizationVoidFailed = new PaymentAuthorizationVoidFailed(
            Guid.NewGuid(),
            Guid.NewGuid(),
            paymentId,
            order.Id,
            "void failed",
            DateTime.UtcNow);
        await harness.Bus.Publish(authorizationVoidFailed);

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.ManualReviewRequired)).Should().NotBeNull();
        var sagaState = sagaHarness.Sagas.ContainsInState(
            order.Id,
            sagaHarness.StateMachine,
            sagaHarness.StateMachine.ManualReviewRequired);
        sagaState.Should().NotBeNull();
        sagaState!.FailureReason.Should().Be("void failed");
        sagaState.LastProcessedEventId.Should().Be(authorizationVoidFailed.EventId);
        await integrationEventPublisher.DidNotReceive()
            .PublishAsync(Arg.Any<OrderFailed>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_payment_authorized_event_should_not_publish_duplicate_stock_commit()
    {
        var order = CreateOrder("idem-harness-duplicate-payment-authorized");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        var paymentAuthorized = new PaymentAuthorized(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow);

        await harness.Bus.Publish(paymentAuthorized);
        (await harness.Published.Any<CommitStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(paymentAuthorized);

        PublishedCount<CommitStockRequested>(harness, order.Id).Should().Be(1);
        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.StockCommitRequested)).Should().NotBeNull();
    }

    [Fact]
    public async Task Out_of_order_stock_committed_event_should_be_ignored_while_waiting_for_payment()
    {
        var order = CreateOrder("idem-harness-out-of-order-stock-committed");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await harness.Bus.Publish(new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));

        (await harness.Consumed.Any<StockCommitted>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();
        PublishedCount<CapturePaymentRequested>(harness, order.Id).Should().Be(0);
        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.WaitingForPayment)).Should().NotBeNull();
    }

    [Fact]
    public async Task Redelivered_stock_committed_event_should_not_publish_duplicate_payment_capture()
    {
        var order = CreateOrder("idem-harness-redelivered-stock-committed");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));
        (await harness.Published.Any<CommitStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        var stockCommitted = new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow);
        await harness.Bus.Publish(stockCommitted);
        (await harness.Published.Any<CapturePaymentRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(stockCommitted);

        PublishedCount<CapturePaymentRequested>(harness, order.Id).Should().Be(1);
        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.CaptureRequested)).Should().NotBeNull();
    }

    [Fact]
    public async Task Final_state_should_ignore_late_checkout_events()
    {
        var order = CreateOrder("idem-harness-final-state-ignore");
        var paymentId = Guid.NewGuid();
        var integrationEventPublisher = Substitute.For<IIntegrationEventPublisher>();
        await using var provider = CreateProvider(integrationEventPublisher);
        await SeedOrderAsync(provider, order);
        var (harness, sagaHarness) = await StartHarnessAsync(provider);

        await StartCheckoutAsync(harness, sagaHarness, order.Id, paymentId);
        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));
        (await harness.Published.Any<CommitStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();
        await harness.Bus.Publish(new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        (await harness.Published.Any<CapturePaymentRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();
        await harness.Bus.Publish(new PaymentCaptured(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));
        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.Completed)).Should().NotBeNull();

        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));
        await harness.Bus.Publish(new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        await harness.Bus.Publish(new PaymentCaptured(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));

        (await sagaHarness.Exists(order.Id, sagaHarness.StateMachine.Completed)).Should().NotBeNull();
        PublishedCount<CommitStockRequested>(harness, order.Id).Should().Be(1);
        PublishedCount<CapturePaymentRequested>(harness, order.Id).Should().Be(1);
        await integrationEventPublisher.Received(1).PublishAsync(
            Arg.Is<OrderConfirmed>(message => message.OrderId == order.Id),
            Arg.Any<CancellationToken>());
    }

    private static ServiceProvider CreateProvider(IIntegrationEventPublisher integrationEventPublisher)
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString("N");

        services.AddDbContext<OrderDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddSingleton(integrationEventPublisher);
        services.AddScoped<RequestStockCommitActivity>();
        services.AddScoped<RequestPaymentCaptureActivity>();
        services.AddScoped<ConfirmOrderActivity>();
        services.AddScoped<RequestStockReleaseAfterPaymentFailureActivity>();
        services.AddScoped<RequestStockReleaseAfterStockCommitFailureActivity>();
        services.AddScoped<FailOrderPaymentAfterStockReleaseActivity>();
        services.AddScoped<RequestAuthorizationVoidAfterStockReleaseActivity>();
        services.AddScoped<FailOrderAfterAuthorizationVoidActivity>();
        services.AddScoped<RequestCommittedStockReverseAfterPaymentCaptureFailureActivity>();
        services.AddScoped<RequestAuthorizationVoidAfterCommittedStockReverseActivity>();
        services.AddScoped<RequestPendingPaymentCancellationAfterTimeoutActivity>();
        services.AddScoped<RequestStockReleaseAfterPaymentCancellationActivity>();
        services.AddMassTransitTestHarness(configuration =>
        {
            configuration.SetTestTimeouts(
                testTimeout: TimeSpan.FromSeconds(10),
                testInactivityTimeout: TimeSpan.FromSeconds(2));
            configuration.AddConfigureEndpointsCallback((_, _, cfg) =>
            {
                cfg.UseMessageRetry(retry => retry.Intervals(
                    TimeSpan.FromMilliseconds(50),
                    TimeSpan.FromMilliseconds(100),
                    TimeSpan.FromMilliseconds(200)));
            });
            configuration.AddSagaStateMachine<OrderCheckoutStateMachine, OrderCheckoutSagaState>();
        });

        return services.BuildServiceProvider(true);
    }

    private static async Task SeedOrderAsync(ServiceProvider provider, Order.Domain.Entities.Order order)
    {
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        context.Orders.Add(order);
        await context.SaveChangesAsync();
    }

    private static async Task<(
        ITestHarness Harness,
        ISagaStateMachineTestHarness<OrderCheckoutStateMachine, OrderCheckoutSagaState> SagaHarness)> StartHarnessAsync(
        ServiceProvider provider)
    {
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        return (
            harness,
            harness.GetSagaStateMachineHarness<OrderCheckoutStateMachine, OrderCheckoutSagaState>());
    }

    private static async Task StartCheckoutAsync(
        ITestHarness harness,
        ISagaStateMachineTestHarness<OrderCheckoutStateMachine, OrderCheckoutSagaState> sagaHarness,
        Guid orderId,
        Guid paymentId)
    {
        await harness.Bus.Publish(new OrderCheckoutStarted(Guid.NewGuid(), orderId, paymentId, DateTime.UtcNow));
        var sagaId = await sagaHarness.Exists(orderId, sagaHarness.StateMachine.WaitingForPayment);

        if (!sagaId.HasValue)
        {
            var fault = harness.Published.Select<Fault<OrderCheckoutStarted>>().FirstOrDefault();
            var faultMessage = fault is null
                ? "Order checkout saga was not created and no MassTransit fault was published."
                : string.Join(
                    " | ",
                    fault.Context.Message.Exceptions.Select(FormatException));

            throw new XunitException(faultMessage);
        }
    }

    private static string FormatException(ExceptionInfo exception)
    {
        var current = $"{exception.ExceptionType}: {exception.Message}";

        return exception.InnerException is null
            ? current
            : $"{current} -> {FormatException(exception.InnerException)}";
    }

    private static string GetFaultMessage<TMessage>(ITestHarness harness)
        where TMessage : class
    {
        var fault = harness.Published.Select<Fault<TMessage>>().FirstOrDefault();

        return fault is null
            ? $"{typeof(TMessage).Name} did not produce the expected message and no MassTransit fault was published."
            : string.Join(" | ", fault.Context.Message.Exceptions.Select(FormatException));
    }

    private static int PublishedCount<TMessage>(ITestHarness harness, Guid orderId)
        where TMessage : class
    {
        return harness.Published.Select<TMessage>().Count(x => HasOrderId(x.Context.Message, orderId));
    }

    private static bool HasOrderId<TMessage>(TMessage message, Guid orderId)
        where TMessage : class
    {
        var orderIdProperty = typeof(TMessage).GetProperty("OrderId");

        return orderIdProperty?.GetValue(message) is Guid messageOrderId && messageOrderId == orderId;
    }

    private static async Task ReachStockReverseRequestedAfterCaptureFailureAsync(
        ITestHarness harness,
        Order.Domain.Entities.Order order,
        Guid paymentId)
    {
        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            DateTime.UtcNow));
        (await harness.Published.Any<CommitStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new StockCommitted(Guid.NewGuid(), Guid.NewGuid(), order.Id, DateTime.UtcNow));
        (await harness.Published.Any<CapturePaymentRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();

        await harness.Bus.Publish(new PaymentCaptureFailed(
            Guid.NewGuid(),
            paymentId,
            order.Id,
            order.TotalAmount,
            order.Currency,
            "capture failed",
            DateTime.UtcNow));
        (await harness.Published.Any<ReverseCommittedStockRequested>(x => x.Context.Message.OrderId == order.Id))
            .Should().BeTrue();
    }

    private static Order.Domain.Entities.Order CreateOrder(string idempotencyKey)
    {
        return new Order.Domain.Entities.Order(
            Guid.NewGuid(),
            idempotencyKey,
            [
                new OrderLineSnapshot(Guid.NewGuid(), "SKU-1", "Product 1", "Variant 1", new Money(100m, "TRY"), 1)
            ]);
    }
}
