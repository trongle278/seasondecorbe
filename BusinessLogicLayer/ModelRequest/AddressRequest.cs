using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DataAccessObject.Models.Address;

namespace BusinessLogicLayer.ModelRequest
{
    public class AddressRequest
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public AddressType Type { get; set; }
        public bool IsDefault { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Street { get; set; }
        public string Detail { get; set; }
        public bool IsDelete { get; set; }
    }
}
