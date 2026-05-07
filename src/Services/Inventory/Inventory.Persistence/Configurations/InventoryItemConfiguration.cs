using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Persistence.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("inventory_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.Sku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.TotalQuantity)
            .IsRequired();

        builder.Property(x => x.ReservedQuantity)
            .IsRequired();

        builder.Ignore(x => x.AvailableQuantity);

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP AT TIME ZONE 'UTC'");

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedBy)
            .HasMaxLength(100);

        builder.HasMany(x => x.Reservations)
            .WithOne()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Reservations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(x => x.StockMovements)
            .WithOne()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.StockMovements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.ProductId)
            .IsUnique();

        builder.HasIndex(x => x.Sku)
            .IsUnique();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
