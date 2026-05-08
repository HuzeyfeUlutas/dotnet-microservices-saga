namespace Notification.Application.Abstractions.Email;

public interface IEmailSender
{
    string ProviderName { get; }

    Task<EmailSendResult> SendAsync(
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken = default);
}
