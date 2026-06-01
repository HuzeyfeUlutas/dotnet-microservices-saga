namespace Payment.Domain.Enums;

public enum PaymentStatus
{
    Pending = 1,
    RequiresAction = 2,
    Authorized = 3,
    AuthorizationFailed = 4,
    Captured = 5,
    CaptureFailed = 6,
    Refunded = 7,
    RefundFailed = 8,
    Cancelled = 9,
    AuthorizationVoided = 10,
    AuthorizationVoidFailed = 11
}
