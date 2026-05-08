using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Notification.Domain.Entities;

namespace Notification.Persistence.Configurations;

public class NotificationMessageConfiguration : IEntityTypeConfiguration<NotificationMessage>
{
    public void Configure(EntityTypeBuilder<NotificationMessage> builder)
    {
        builder.ToTable("notification_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Channel)
            .IsRequired();

        builder.Property(x => x.NotificationType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Recipient)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(x => x.Body)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(500);

        builder.Property(x => x.CancellationReason)
            .HasMaxLength(500);

        builder.Property(x => x.SkipReason)
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

        builder.HasMany(x => x.DeliveryAttempts)
            .WithOne()
            .HasForeignKey(x => x.NotificationMessageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.DeliveryAttempts)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.SourceEventId);
        builder.HasIndex(x => x.CorrelationId);
        builder.HasIndex(x => new { x.SourceEventId, x.NotificationType, x.Recipient })
            .IsUnique()
            .HasFilter("\"SourceEventId\" IS NOT NULL");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
