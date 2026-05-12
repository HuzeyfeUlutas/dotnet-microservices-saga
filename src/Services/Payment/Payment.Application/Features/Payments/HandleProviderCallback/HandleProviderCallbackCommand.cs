using MediatR;
using Payment.Application.DTOs;

namespace Payment.Application.Features.Payments.HandleProviderCallback;

public sealed record HandleProviderCallbackCommand(
    Guid PaymentId,
    bool Approved,
    string? ProviderEventId = null) : IRequest<PaymentDto>;
