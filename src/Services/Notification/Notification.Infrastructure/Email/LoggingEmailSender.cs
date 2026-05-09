using Microsoft.Extensions.Logging;
using Notification.Application.Abstractions.Email;

namespace Notification.Infrastructure.Email;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    private const string ProviderName = "Logging";

    string IEmailSender.ProviderName => ProviderName;

    public Task<EmailSendResult> SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Email send simulated for {Recipient} with subject {Subject} and body length {BodyLength}",
            recipient,
            subject,
            body.Length);

        return Task.FromResult(EmailSendResult.Success(ProviderName));
    }
}
