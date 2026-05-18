using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Notification.Infrastructure.Configuration;
using Notification.Infrastructure.Email;
using Xunit;

namespace Notification.Infrastructure.Tests.Email;

public class LoggingEmailSenderTests
{
    [Fact]
    public async Task SendAsync_should_return_failure_for_configured_failure_recipient()
    {
        var sender = new LoggingEmailSender(
            NullLogger<LoggingEmailSender>.Instance,
            Options.Create(new EmailDeliveryOptions
            {
                ProviderName = "FakeProvider",
                FailureRecipients = ["blocked@example.com"]
            }));

        var result = await sender.SendAsync("blocked@example.com", "Subject", "Body", CancellationToken.None);

        result.Failed.Should().BeTrue();
        result.Provider.Should().Be("FakeProvider");
        result.ErrorMessage.Should().Be("Recipient is configured for simulated delivery failure.");
    }
}
