using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse.Payment
{
    public class TransactionsResponse
    {
        public int Id { get; set; }
        public int? BookingId { get; set; }
        public int? OrderId { get; set; }
        public int PaymentTransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string TransactionStatus { get; set; }
        public bool IsMoneyIn { get; set; }
        public decimal SignedAmount => IsMoneyIn ? Amount : -Amount;
    }
}
