using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;

namespace BusinessLogicLayer.ModelResponse
{
    public class RoleResponse : BaseResponse
    {
        public RoleDTO Data { get; set; }
    }

    public class RoleListResponse : BaseResponse
    {
        public List<RoleDTO> Data { get; set; }
    }
}
