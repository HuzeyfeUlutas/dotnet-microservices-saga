using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Entities;

namespace Notification.Persistence.Configurations;

public class RecipientPreferenceConfiguration : IEntityTypeConfiguration<RecipientPreference>
{
    public void Configure(EntityTypeBuilder<RecipientPreference> builder)
    {
        builder.ToTable("recipient_preferences");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecipientId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Channel)
            .IsRequired();

        builder.Property(x => x.NotificationType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.DisabledReason)
            .HasMaxLength(500);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedBy)
            .HasMaxLength(100);

        builder.HasIndex(x => new { x.RecipientId, x.Channel, x.NotificationType })
            .IsUnique();

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
