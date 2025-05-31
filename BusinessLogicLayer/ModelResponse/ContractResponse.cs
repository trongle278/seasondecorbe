using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelResponse
{
    public class ContractResponse
    {
        public Contract Contract { get; set; }
        public string FileUrl { get; set; }
    }

    public class ContractContentResponse
    {
        public string ContractCode { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TermOfUseContent { get; set; }
    }

    public class ContractFileResponse
    {
        public string ContractCode { get; set; }
        public string QuotationCode { get; set; }
        public int Status { get; set; }
        public bool? IsSigned { get; set; }
        public bool? IsDeposited { get; set; }
        public bool? IsFinalPaid { get; set; }
        public bool? IsTerminatable { get; set; }
        public string FileUrl { get; set; }
        public string BookingCode { get; set; }
        public string Note { get; set; }
        public DateTime SurveyDate { get; set; }
        public DateTime? ConstructionDate { get; set; }
        public DateTime? SignedDate { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }

        public string BusinessName { get; set; }
        public string ProviderName { get; set; }
        public string ProviderEmail { get; set; }
        public string ProviderPhone { get; set; }
    }

    public class ContractCancelDetailResponse
    {
        public string ContractCode { get; set; }
        public int Status { get; set; }
        public string CancelType { get; set; }
        public string Reason { get; set; }
    }
}
