using FluentAssertions;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Exceptions;
using Xunit;

namespace Notification.Domain.Tests;

public class RecipientPreferenceTests
{
    [Fact]
    public void Constructor_should_initialize_enabled_preference()
    {
        var preference = new RecipientPreference("  user-1  ", NotificationChannel.Email, "  OrderConfirmed  ");

        preference.RecipientId.Should().Be("user-1");
        preference.Channel.Should().Be(NotificationChannel.Email);
        preference.NotificationType.Should().Be("OrderConfirmed");
        preference.IsEnabled.Should().BeTrue();
        preference.DisabledReason.Should().BeNull();
    }

    [Fact]
    public void Disable_should_set_reason()
    {
        var preference = new RecipientPreference("user-1", NotificationChannel.Email, "OrderConfirmed");

        preference.Disable("  user opted out  ");

        preference.IsEnabled.Should().BeFalse();
        preference.DisabledReason.Should().Be("user opted out");
    }

    [Fact]
    public void Disable_should_reject_empty_reason()
    {
        var preference = new RecipientPreference("user-1", NotificationChannel.Email, "OrderConfirmed");

        var action = () => preference.Disable(" ");

        action.Should().Throw<DomainException>()
            .WithMessage("Disabled reason cannot be empty.");
    }
}
