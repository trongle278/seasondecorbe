using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessObject.Models
{
    public class Contract
    {
        public int Id { get; set; }
        public string ContractCode { get; set; }
        public int QuotationId { get; set; }
        public Quotation Quotation { get; set; }

        public string TermOfUseContent { get; set; }
        public string? ContractFilePath { get; set; }
        public DateTime CreatedAt { get; set; }
        public ContractStatus Status { get; set; }
        public DateTime? SignedDate { get; set; }
        public string? SignatureToken { get; set; }
        public DateTime? SignatureTokenGeneratedAt { get; set; }
        public bool isContractExisted { get; set; }
        public bool? isSigned { get; set; }
        public bool? isDeposited { get; set; }
        public bool? isFinalPaid { get; set; }

        public bool? isTerminatable { get; set; }

        public enum ContractStatus
        {
            Pending,    // Chờ ký
            Signed,     // Đã ký
            Rejected,   // Từ chối ký
            PendingCancel,
            Canceled   // Đã hủy, cho trường hợp 2 bên và cả 1 bên đơn phương
        }

        public string? Reason { get; set; }
    }

}
