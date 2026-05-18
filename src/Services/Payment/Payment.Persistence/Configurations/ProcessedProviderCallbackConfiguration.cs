using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Payment.Domain.Entities;

namespace Payment.Persistence.Configurations;

public class ProcessedProviderCallbackConfiguration : IEntityTypeConfiguration<ProcessedProviderCallback>
{
    public void Configure(EntityTypeBuilder<ProcessedProviderCallback> builder)
    {
        builder.ToTable("processed_provider_callbacks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentId)
            .IsRequired();

        builder.Property(x => x.Provider)
            .IsRequired();

        builder.Property(x => x.ProviderEventId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ProcessedAtUtc)
            .IsRequired();

        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => new { x.Provider, x.ProviderEventId })
            .IsUnique();
    }
}
