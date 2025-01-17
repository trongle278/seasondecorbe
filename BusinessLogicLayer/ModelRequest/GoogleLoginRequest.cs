using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class GoogleLoginRequest
    {
        public string Credential { get; set; }
        public int? RoleId { get; set; }
    }
}
