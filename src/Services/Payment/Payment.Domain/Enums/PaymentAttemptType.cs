namespace Payment.Domain.Enums;

public enum PaymentAttemptType
{
    Authorization = 1,
    Capture = 2,
    Refund = 3,
    AuthorizationVoid = 4
}
