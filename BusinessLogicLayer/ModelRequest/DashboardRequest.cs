using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataAccessObject.Models.PaymentTransaction;

namespace BusinessLogicLayer.ModelRequest
{
    public class ProviderPaymentFilterRequest
    {
        public AllowedTransactionType? TransactionType { get; set; }
        public int PageIndex { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? SortBy { get; set; } = "TransactionDate";
        public bool Descending { get; set; } = false;

        public enum AllowedTransactionType
        {
            Deposit = 2,
            FinalPay = 4,
            //Revenue = 5,
            OrderPay = 6,
            PenaltyPay = 7
        }
    }
}
