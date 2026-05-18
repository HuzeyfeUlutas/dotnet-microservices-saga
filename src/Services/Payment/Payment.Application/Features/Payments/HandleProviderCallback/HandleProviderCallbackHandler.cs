using MediatR;
using Payment.Application.DTOs;
using Payment.Application.Features.Payments.CompleteFake3ds;

namespace Payment.Application.Features.Payments.HandleProviderCallback;

public class HandleProviderCallbackHandler(ISender sender) : IRequestHandler<HandleProviderCallbackCommand, PaymentDto>
{
    public Task<PaymentDto> Handle(HandleProviderCallbackCommand request, CancellationToken cancellationToken)
    {
        return sender.Send(
            new CompleteFake3dsCommand(request.PaymentId, request.Approved, request.ProviderEventId),
            cancellationToken);
    }
}
