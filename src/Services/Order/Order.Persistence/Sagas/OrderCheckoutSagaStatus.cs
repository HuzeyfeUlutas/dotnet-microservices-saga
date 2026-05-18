namespace Order.Persistence.Sagas;

public static class OrderCheckoutSagaStatus
{
    public const string WaitingForPayment = "WaitingForPayment";
    public const string PaymentAuthorized = "PaymentAuthorized";
    public const string CaptureRequested = "CaptureRequested";
    public const string PaymentFailed = "PaymentFailed";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}
