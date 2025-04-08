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
}
