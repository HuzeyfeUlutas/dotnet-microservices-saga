using Payment.Domain.Common;
using Payment.Domain.Enums;
using Payment.Domain.Exceptions;

namespace Payment.Domain.Entities;

public class ProcessedProviderCallback : BaseEntity<Guid>
{
    private ProcessedProviderCallback()
    {
    }

    public ProcessedProviderCallback(
        Guid paymentId,
        PaymentProviderType provider,
        string providerEventId) : base(Guid.NewGuid())
    {
        if (paymentId == Guid.Empty)
        {
            throw new DomainException("Payment id cannot be empty.");
        }

        PaymentId = paymentId;
        Provider = provider;
        ProviderEventId = NormalizeRequired(providerEventId, "Provider event id cannot be empty.");
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public Guid PaymentId { get; private set; }
    public PaymentProviderType Provider { get; private set; }
    public string ProviderEventId { get; private set; } = null!;
    public DateTime ProcessedAtUtc { get; private set; }

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(message);
        }

        return value.Trim();
    }
}
