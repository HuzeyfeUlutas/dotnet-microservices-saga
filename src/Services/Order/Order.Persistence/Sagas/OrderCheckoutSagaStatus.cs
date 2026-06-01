namespace Order.Persistence.Sagas;

public static class OrderCheckoutSagaStatus
{
    public const string WaitingForPayment = "WaitingForPayment";
    public const string StockCommitRequested = "StockCommitRequested";
    public const string StockReleaseRequestedAfterPaymentFailure = "StockReleaseRequestedAfterPaymentFailure";
    public const string StockReleaseRequestedAfterStockCommitFailure = "StockReleaseRequestedAfterStockCommitFailure";
    public const string StockReleasedAfterStockCommitFailure = "StockReleasedAfterStockCommitFailure";
    public const string CaptureRequested = "CaptureRequested";
    public const string PaymentFailed = "PaymentFailed";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
    public const string ManualReviewRequired = "ManualReviewRequired";
}
