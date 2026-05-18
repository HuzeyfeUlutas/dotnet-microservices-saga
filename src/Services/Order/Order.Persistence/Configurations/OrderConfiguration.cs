using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;
using OrderEntity = Order.Domain.Entities.Order;

namespace Order.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BuyerId)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.PaymentId);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.TotalAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.BuyerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PaymentId)
            .IsUnique();
        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique();
    }
}
