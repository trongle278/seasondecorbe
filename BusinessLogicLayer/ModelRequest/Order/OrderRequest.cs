using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.ModelRequest.Order
{
    public class OrderRequest
    {
        public string Phone { get; set; }
        public string FullName { get; set; }
    }
}
