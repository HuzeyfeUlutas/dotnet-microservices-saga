using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Notification.Application.Features.NotificationPreferences.SetRecipientPreference;
using Notification.Application.Tests.Support;
using Notification.Domain.Enums;
using Xunit;

namespace Notification.Application.Tests.Features.NotificationPreferences;

public class SetRecipientPreferenceHandlerTests
{
    [Fact]
    public async Task Handle_should_create_disabled_preference_with_reason()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var handler = new SetRecipientPreferenceHandler(context);

        await handler.Handle(
            new SetRecipientPreferenceCommand(
                "user-1",
                NotificationChannel.Email,
                "OrderConfirmed",
                false,
                "user opted out"),
            CancellationToken.None);

        var preference = await context.RecipientPreferences.SingleAsync();
        preference.RecipientId.Should().Be("user-1");
        preference.IsEnabled.Should().BeFalse();
        preference.DisabledReason.Should().Be("user opted out");
    }

    [Fact]
    public async Task Handle_should_enable_existing_preference()
    {
        using var factory = new NotificationTestDbContextFactory();
        await using var context = factory.CreateContext();
        var preference = new Notification.Domain.Entities.RecipientPreference(
            "user-1",
            NotificationChannel.Email,
            "OrderConfirmed",
            false);
        preference.Disable("user opted out");
        context.RecipientPreferences.Add(preference);
        await context.SaveChangesAsync();

        var handler = new SetRecipientPreferenceHandler(context);

        await handler.Handle(
            new SetRecipientPreferenceCommand(
                "user-1",
                NotificationChannel.Email,
                "OrderConfirmed",
                true,
                null),
            CancellationToken.None);

        var storedPreference = await context.RecipientPreferences.SingleAsync();
        storedPreference.IsEnabled.Should().BeTrue();
        storedPreference.DisabledReason.Should().BeNull();
    }
}
