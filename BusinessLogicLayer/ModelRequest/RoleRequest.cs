using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelRequest
{
    public class RoleDTO
    {
        public int Id { get; set; }
        public string RoleName { get; set; }
    }

    public class CreateRoleRequest
    {
        public string RoleName { get; set; }
    }

    public class UpdateRoleRequest
    {
        public string RoleName { get; set; }
    }
}
