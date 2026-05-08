using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;

namespace Notification.Application.Abstractions.Persistence;

public interface INotificationDbContext
{
    DbSet<NotificationMessage> NotificationMessages { get; }
    DbSet<NotificationDeliveryAttempt> NotificationDeliveryAttempts { get; }
    DbSet<NotificationTemplate> NotificationTemplates { get; }
    DbSet<RecipientPreference> RecipientPreferences { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
