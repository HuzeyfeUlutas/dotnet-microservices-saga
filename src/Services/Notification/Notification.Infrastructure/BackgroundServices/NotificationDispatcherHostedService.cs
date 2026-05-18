using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Common.Exceptions;
using Notification.Application.Features.Notifications.SendNotification;
using Notification.Domain.Enums;
using Notification.Infrastructure.Configuration;
using Notification.Persistence.Context;

namespace Notification.Infrastructure.BackgroundServices;

public sealed class NotificationDispatcherHostedService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<NotificationDeliveryOptions> options,
    ILogger<NotificationDispatcherHostedService> logger) : BackgroundService
{
    private readonly NotificationDeliveryOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Notification dispatcher started with interval {IntervalSeconds}s and batch size {BatchSize}",
            _options.DispatcherIntervalSeconds,
            _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchPendingNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Notification dispatcher iteration failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.DispatcherIntervalSeconds), stoppingToken);
        }
    }

    private async Task DispatchPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var now = DateTime.UtcNow;
        var notificationIds = await dbContext.NotificationMessages
            .AsNoTracking()
            .Where(x => x.Status == NotificationMessageStatus.Pending)
            .Where(x => !x.ScheduledAtUtc.HasValue || x.ScheduledAtUtc <= now)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (notificationIds.Count == 0)
        {
            return;
        }

        logger.LogInformation("Notification dispatcher picked up {Count} pending notifications.", notificationIds.Count);

        foreach (var notificationId in notificationIds)
        {
            try
            {
                await sender.Send(new SendNotificationCommand(notificationId), cancellationToken);
            }
            catch (ConflictException exception)
            {
                logger.LogWarning(
                    exception,
                    "Notification dispatcher skipped conflicted notification {NotificationId}",
                    notificationId);
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Notification dispatcher failed while processing notification {NotificationId}",
                    notificationId);
            }
        }
    }
}
