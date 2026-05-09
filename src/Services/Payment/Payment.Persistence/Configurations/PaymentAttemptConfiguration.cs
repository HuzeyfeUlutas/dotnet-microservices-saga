using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Persistence.Configurations;

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("payment_attempts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentId)
            .IsRequired();

        builder.Property(x => x.AttemptNumber)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();

        builder.Property(x => x.Provider)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(150);

        builder.Property(x => x.ProviderPaymentId)
            .HasMaxLength(150);

        builder.Property(x => x.ProviderTransactionId)
            .HasMaxLength(150);

        builder.Property(x => x.ProviderActionReference)
            .HasMaxLength(500);

        builder.Property(x => x.StartedAtUtc)
            .IsRequired();

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => new { x.PaymentId, x.AttemptNumber })
            .IsUnique();
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        builder.HasIndex(x => x.ProviderPaymentId);
        builder.HasIndex(x => x.ProviderTransactionId);
    }
}
