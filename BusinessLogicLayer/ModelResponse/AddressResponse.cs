using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class AddressResponse
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string AddressType { get; set; }
        public bool IsDefault { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Street { get; set; }
        public string Detail { get; set; } // Added property
        public bool IsDelete { get; set; }
    }

    public class OrderAddressResponse
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string AddressType { get; set; }
        public bool IsDefault { get; set; }
        public string Province { get; set; }
        public string District { get; set; }
        public string Ward { get; set; }
        public string Street { get; set; }
        public string Detail { get; set; }
    }
}
