using FluentAssertions;
using Grpc.Core;
using Marketplace.Grpc.Payment.V1;
using MediatR;
using NSubstitute;
using Payment.API.Grpc.Services;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.CreatePayment;
using Payment.Domain.Enums;
using Xunit;

namespace Payment.API.Tests.Grpc;

public class PaymentInitiationGrpcServiceTests
{
    [Fact]
    public async Task CreatePayment_ShouldDelegateToApplicationCommandAndMapResponse()
    {
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<CreatePaymentCommand>(), Arg.Any<CancellationToken>())
            .Returns(new CreatePaymentResultDto(
                new PaymentDto(
                    paymentId,
                    orderId,
                    199.90m,
                    "TRY",
                    PaymentProviderType.Fake,
                    PaymentMethodType.Card,
                    PaymentStatus.RequiresAction,
                    "idem-1",
                    DateTime.UtcNow,
                    null,
                    null,
                    null,
                    null),
                new PaymentActionDto("Redirect", "/fake-3ds/payments/test")));
        var service = new PaymentInitiationGrpcService(sender);

        var response = await service.CreatePayment(
            new CreatePaymentRequest
            {
                OrderId = orderId.ToString(),
                Amount = "199.90",
                Currency = "TRY",
                IdempotencyKey = "idem-1",
                Provider = 1,
                Method = 1
            },
            Substitute.For<ServerCallContext>());

        response.PaymentId.Should().Be(paymentId.ToString());
        response.Status.Should().Be((int)PaymentStatus.RequiresAction);
        response.Action.RedirectUrl.Should().Be("/fake-3ds/payments/test");
        await sender.Received(1).Send(
            Arg.Is<CreatePaymentCommand>(command =>
                command.OrderId == orderId &&
                command.Amount == 199.90m &&
                command.Provider == PaymentProviderType.Fake &&
                command.Method == PaymentMethodType.Card),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreatePayment_ShouldRejectInvalidAmount()
    {
        var service = new PaymentInitiationGrpcService(Substitute.For<ISender>());

        var action = async () => await service.CreatePayment(
            new CreatePaymentRequest
            {
                OrderId = Guid.NewGuid().ToString(),
                Amount = "invalid",
                Currency = "TRY",
                IdempotencyKey = "idem-1",
                Provider = 1,
                Method = 1
            },
            Substitute.For<ServerCallContext>());

        var exception = await action.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
