using Microsoft.EntityFrameworkCore;
using Payment.Persistence.Context;

namespace Payment.Application.Tests.Support;

internal sealed class ConcurrencyFailingPaymentDbContext(DbContextOptions<PaymentDbContext> options)
    : PaymentDbContext(options)
{
    public bool FailNextSave { get; set; }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!FailNextSave)
        {
            return base.SaveChangesAsync(cancellationToken);
        }

        FailNextSave = false;
        throw new DbUpdateConcurrencyException("Simulated concurrency conflict.");
    }
}
