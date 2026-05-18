namespace Notification.Application.Abstractions.Email;

public enum EmailSendStatus
{
    Succeeded = 1,
    Failed = 2,
    Skipped = 3
}

public sealed record EmailSendResult(
    EmailSendStatus Status,
    string Provider,
    string? ProviderMessageId,
    string? ErrorMessage)
{
    public bool Succeeded => Status == EmailSendStatus.Succeeded;
    public bool Failed => Status == EmailSendStatus.Failed;
    public bool Skipped => Status == EmailSendStatus.Skipped;

    public static EmailSendResult Success(string provider, string? providerMessageId = null)
    {
        return new EmailSendResult(EmailSendStatus.Succeeded, provider, providerMessageId, null);
    }

    public static EmailSendResult Failure(string provider, string errorMessage)
    {
        return new EmailSendResult(EmailSendStatus.Failed, provider, null, errorMessage);
    }

    public static EmailSendResult SkippedResult(string provider, string message)
    {
        return new EmailSendResult(EmailSendStatus.Skipped, provider, null, message);
    }
}
