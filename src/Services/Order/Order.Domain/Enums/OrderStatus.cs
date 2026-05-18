namespace Order.Domain.Enums;

public enum OrderStatus
{
    WaitingForPayment = 1,
    Confirmed = 2,
    PaymentFailed = 3,
    Failed = 4
}
