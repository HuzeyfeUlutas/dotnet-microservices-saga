using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using PaymentEntity = Payment.Domain.Entities.Payment;

namespace Payment.Application.Abstractions.Persistence;

public interface IPaymentDbContext
{
    DbSet<PaymentEntity> Payments { get; }
    DbSet<PaymentAttempt> PaymentAttempts { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
