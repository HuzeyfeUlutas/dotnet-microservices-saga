using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Order.Domain.Entities;

namespace Order.Persistence.Configurations;

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("order_lines");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ProductName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.VariantName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .IsRequired()
            .HasMaxLength(3);

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.LineTotal)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => new { x.OrderId, x.ProductId, x.Sku });
    }
}
