namespace Notification.Application.Abstractions.Email;

public sealed record EmailSendResult(
    bool Succeeded,
    string Provider,
    string? ProviderMessageId,
    string? ErrorMessage)
{
    public static EmailSendResult Success(string provider, string? providerMessageId = null)
    {
        return new EmailSendResult(true, provider, providerMessageId, null);
    }

    public static EmailSendResult Failure(string provider, string errorMessage)
    {
        return new EmailSendResult(false, provider, null, errorMessage);
    }
}
