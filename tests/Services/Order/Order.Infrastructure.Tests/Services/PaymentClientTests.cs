using FluentAssertions;
using Grpc.Core;
using Marketplace.Grpc.Payment.V1;
using Order.Application.Common.Exceptions;
using Order.Infrastructure.Configuration;
using Order.Infrastructure.Services;
using Xunit;

namespace Order.Infrastructure.Tests.Services;

public class PaymentClientTests
{
    [Fact]
    public async Task CreatePaymentAsync_ShouldMapGrpcRequestAndResponse()
    {
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();
        var grpcClient = new StubPaymentInitiationGrpcClient
        {
            Response = new CreatePaymentResponse
            {
                PaymentId = paymentId.ToString(),
                Status = 2,
                Provider = 1,
                Action = new Marketplace.Grpc.Payment.V1.PaymentAction
                {
                    Type = "Redirect",
                    RedirectUrl = "/fake-3ds/payments/test"
                }
            }
        };
        var client = CreateClient(grpcClient);

        var result = await client.CreatePaymentAsync(
            orderId,
            199.90m,
            "TRY",
            "idem-1",
            "Fake",
            "Card");

        result.PaymentId.Should().Be(paymentId);
        result.Status.Should().Be("RequiresAction");
        result.Provider.Should().Be("Fake");
        result.Action.RedirectUrl.Should().Be("/fake-3ds/payments/test");
        grpcClient.Request.Should().NotBeNull();
        grpcClient.Request!.OrderId.Should().Be(orderId.ToString());
        grpcClient.Request.Amount.Should().Be("199.90");
        grpcClient.Deadline.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldMapFailedPreconditionRpcStatus()
    {
        var grpcClient = new StubPaymentInitiationGrpcClient
        {
            Exception = new RpcException(new Status(StatusCode.FailedPrecondition, "Payment cannot be initiated."))
        };
        var client = CreateClient(grpcClient);

        var action = async () => await client.CreatePaymentAsync(
            Guid.NewGuid(),
            199.90m,
            "TRY",
            "idem-1",
            "Fake",
            "Card");

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Payment initiation failed. Payment cannot be initiated.");
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldRejectUnsupportedProvider()
    {
        var client = CreateClient(new StubPaymentInitiationGrpcClient());

        var action = async () => await client.CreatePaymentAsync(
            Guid.NewGuid(),
            199.90m,
            "TRY",
            "idem-1",
            "Unsupported",
            "Card");

        await action.Should().ThrowAsync<ConflictException>()
            .WithMessage("Payment provider 'Unsupported' is not supported.");
    }

    private static PaymentClient CreateClient(PaymentInitiation.PaymentInitiationClient grpcClient)
    {
        return new PaymentClient(
            grpcClient,
            new ServiceEndpointOptions
            {
                PaymentGrpcTimeoutSeconds = 3
            });
    }

    private sealed class StubPaymentInitiationGrpcClient : PaymentInitiation.PaymentInitiationClient
    {
        public CreatePaymentRequest? Request { get; private set; }
        public CreatePaymentResponse? Response { get; init; }
        public RpcException? Exception { get; init; }
        public DateTime? Deadline { get; private set; }

        public override AsyncUnaryCall<CreatePaymentResponse> CreatePaymentAsync(
            CreatePaymentRequest request,
            Metadata? headers = null,
            DateTime? deadline = null,
            CancellationToken cancellationToken = default)
        {
            Request = request;
            Deadline = deadline;

            return new AsyncUnaryCall<CreatePaymentResponse>(
                Exception is null
                    ? Task.FromResult(Response!)
                    : Task.FromException<CreatePaymentResponse>(Exception),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        }
    }
}
