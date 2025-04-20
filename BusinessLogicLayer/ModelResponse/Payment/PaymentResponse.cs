using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse.Payment
{
    public class DepositPaymentResponse
    {
        public string QuotationCode { get; set; }
        public string ContractCode { get; set; }
        public decimal DepositAmount { get; set; }

        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }

        public string ProviderName { get; set; }
        public string ProviderEmail { get; set; }
        public string ProviderPhone { get; set; }
        public string ProviderAddress { get; set; }
    }

    public class FinalPaymentResponse
    {
        public string QuotationCode { get; set; }
        public string ContractCode { get; set; }
        public decimal FinalPaymentAmount { get; set; }

        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string CustomerAddress { get; set; }

        public string ProviderName { get; set; }
        public string ProviderEmail { get; set; }
        public string ProviderPhone { get; set; }
        public string ProviderAddress { get; set; }
    }

}
