using static DataAccessObject.Models.PaymentTransaction;

public class VnPayRequest
{
    public int OrderID { get; set; }

    public decimal Amount { get; set; }

    public EnumTransactionType TransactionType { get; set; }

    public EnumTransactionStatus TransactionStatus { get; set; } 

    public int CustomerId { get; set; }

    public int AdminId { get; set; }

    public int ProverId { get; set; }
}