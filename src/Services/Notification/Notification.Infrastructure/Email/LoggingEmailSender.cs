using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Abstractions.Email;
using Notification.Infrastructure.Configuration;

namespace Notification.Infrastructure.Email;

public sealed class LoggingEmailSender(
    ILogger<LoggingEmailSender> logger,
    IOptions<EmailDeliveryOptions> options) : IEmailSender
{
    private readonly EmailDeliveryOptions _options = options.Value;

    string IEmailSender.ProviderName => _options.ProviderName;

    public async Task<EmailSendResult> SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (_options.SimulatedLatencyMs > 0)
        {
            await Task.Delay(_options.SimulatedLatencyMs, cancellationToken);
        }

        if (_options.FailureRecipients.Any(x => string.Equals(x, recipient, StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning(
                "Email send simulated as failure for {Recipient} via provider {ProviderName}",
                recipient,
                _options.ProviderName);

            return EmailSendResult.Failure(_options.ProviderName, "Recipient is configured for simulated delivery failure.");
        }

        logger.LogInformation(
            "Email send simulated for {Recipient} via provider {ProviderName} with subject {Subject} and body length {BodyLength}",
            recipient,
            _options.ProviderName,
            subject,
            body.Length);

        return EmailSendResult.Success(_options.ProviderName);
    }
}
