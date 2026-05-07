using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Persistence.Configurations;

public class InventoryReservationConfiguration : IEntityTypeConfiguration<InventoryReservation>
{
    public void Configure(EntityTypeBuilder<InventoryReservation> builder)
    {
        builder.ToTable("inventory_reservations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.InventoryItemId)
            .IsRequired();

        builder.Property(x => x.OrderId)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.ReservedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.InventoryItemId, x.OrderId });
        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
