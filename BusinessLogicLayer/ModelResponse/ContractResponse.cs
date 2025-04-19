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
        public int Status { get; set; }
        public bool? IsSigned { get; set; }
        public string FileUrl { get; set; }
        public string BookingCode { get; set; }
        public string Note { get; set; }
        public decimal DepositAmount { get; set; }
    }
}
