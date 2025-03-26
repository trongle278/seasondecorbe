using static DataAccessObject.Models.PaymentTransaction;

public class VnPayRequest
{
    public decimal Amount { get; set; }

    public EnumTransactionType TransactionType { get; set; }

    public EnumTransactionStatus TransactionStatus { get; set; } 

    public int CustomerId { get; set; }
}