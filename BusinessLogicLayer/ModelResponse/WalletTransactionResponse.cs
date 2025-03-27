using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse
{
    public class WalletTransactionResponse
    {
        public int Id { get; set; }
        public int WalletId { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public PaymentTransaction.EnumTransactionType TransactionType { get; set; }
        public string TransactionTypeName => TransactionType.ToString();
        public PaymentTransaction.EnumTransactionStatus TransactionStatus { get; set; }
        public string TransactionStatusName => TransactionStatus.ToString();
        public int? BookingId { get; set; }
        public int? OrderId { get; set; }
        public int PaymentTransactionId { get; set; }
    }
}
