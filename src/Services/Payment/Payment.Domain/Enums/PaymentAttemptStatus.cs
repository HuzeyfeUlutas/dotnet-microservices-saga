namespace Payment.Domain.Enums;

public enum PaymentAttemptStatus
{
    Processing = 1,
    RequiresAction = 2,
    Succeeded = 3,
    Failed = 4
}
