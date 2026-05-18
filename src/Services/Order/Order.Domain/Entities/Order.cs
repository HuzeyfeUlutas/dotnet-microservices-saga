using Order.Domain.Common;
using Order.Domain.Enums;
using Order.Domain.Exceptions;
using Order.Domain.ValueObjects;

namespace Order.Domain.Entities;

public class Order : AuditableEntity<Guid>
{
    private readonly List<OrderLine> _lines = [];

    private Order()
    {
    }

    public Order(
        Guid buyerId,
        string idempotencyKey,
        IEnumerable<OrderLineSnapshot> lineSnapshots) : base(Guid.NewGuid())
    {
        if (buyerId == Guid.Empty)
        {
            throw new DomainException("Buyer id cannot be empty.");
        }

        var snapshots = lineSnapshots?.ToList() ?? throw new DomainException("Order lines cannot be empty.");
        if (snapshots.Count == 0)
        {
            throw new DomainException("Order lines cannot be empty.");
        }

        BuyerId = buyerId;
        IdempotencyKey = NormalizeRequired(idempotencyKey, "Idempotency key cannot be empty.");
        Status = OrderStatus.WaitingForPayment;
        CreatedAtUtc = DateTime.UtcNow;

        foreach (var snapshot in snapshots)
        {
            AddLine(snapshot);
        }

        Currency = _lines[0].Currency;
        TotalAmount = decimal.Round(_lines.Sum(x => x.LineTotal), 2, MidpointRounding.AwayFromZero);
    }

    public Guid BuyerId { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public OrderStatus Status { get; private set; }
    public Guid? PaymentId { get; private set; }
    public string Currency { get; private set; } = null!;
    public decimal TotalAmount { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? ConfirmedAtUtc { get; private set; }
    public DateTime? PaymentFailedAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }
    public uint RowVersion { get; private set; }
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public void AttachPayment(Guid paymentId)
    {
        if (paymentId == Guid.Empty)
        {
            throw new DomainException("Payment id cannot be empty.");
        }

        if (PaymentId.HasValue && PaymentId.Value != paymentId)
        {
            throw new DomainException("Order already has a different payment attached.");
        }

        PaymentId = paymentId;
        Touch();
    }

    public void MarkAsConfirmed()
    {
        EnsureStatus(OrderStatus.WaitingForPayment, "Only orders waiting for payment can be confirmed.");

        Status = OrderStatus.Confirmed;
        ConfirmedAtUtc = DateTime.UtcNow;
        FailureReason = null;
        Touch();
    }

    public void MarkPaymentAsFailed(string reason)
    {
        EnsureStatus(OrderStatus.WaitingForPayment, "Only orders waiting for payment can transition to payment failed.");

        Status = OrderStatus.PaymentFailed;
        PaymentFailedAtUtc = DateTime.UtcNow;
        FailureReason = NormalizeRequired(reason, "Failure reason cannot be empty.");
        Touch();
    }

    public void MarkAsFailed(string reason)
    {
        if (Status == OrderStatus.Confirmed)
        {
            throw new DomainException("Confirmed orders cannot transition to failed.");
        }

        if (Status == OrderStatus.Failed)
        {
            throw new DomainException("Order is already failed.");
        }

        Status = OrderStatus.Failed;
        FailedAtUtc = DateTime.UtcNow;
        FailureReason = NormalizeRequired(reason, "Failure reason cannot be empty.");
        Touch();
    }

    private void AddLine(OrderLineSnapshot snapshot)
    {
        if (_lines.Count > 0 && _lines[0].Currency != snapshot.UnitPrice.Currency)
        {
            throw new DomainException("All order lines must use the same currency.");
        }

        var line = new OrderLine(Id, snapshot);
        _lines.Add(line);
    }

    private void EnsureStatus(OrderStatus expectedStatus, string message)
    {
        if (Status != expectedStatus)
        {
            throw new DomainException(message);
        }
    }

    private void Touch()
    {
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static string NormalizeRequired(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(message);
        }

        return value.Trim();
    }
}
