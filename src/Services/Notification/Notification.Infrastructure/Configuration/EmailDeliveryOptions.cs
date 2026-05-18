namespace Notification.Infrastructure.Configuration;

public sealed class EmailDeliveryOptions
{
    public const string SectionName = "EmailDelivery";

    public string ProviderName { get; init; } = "Logging";
    public int SimulatedLatencyMs { get; init; }
    public string[] FailureRecipients { get; init; } = [];
}
