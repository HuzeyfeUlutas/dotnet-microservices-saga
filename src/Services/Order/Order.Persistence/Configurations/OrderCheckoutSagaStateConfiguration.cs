using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Persistence.Sagas;

namespace Order.Persistence.Configurations;

public class OrderCheckoutSagaStateConfiguration : IEntityTypeConfiguration<OrderCheckoutSagaState>
{
    public void Configure(EntityTypeBuilder<OrderCheckoutSagaState> builder)
    {
        builder.ToTable("order_checkout_saga_states");

        builder.HasKey(x => x.CorrelationId);

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.PaymentId)
            .IsRequired();

        builder.Property(x => x.CurrentState)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasIndex(x => x.OrderId)
            .IsUnique();

        builder.HasIndex(x => x.PaymentId)
            .IsUnique();
    }
}
