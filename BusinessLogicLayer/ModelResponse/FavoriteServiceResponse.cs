using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class FavoriteServiceResponse
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int DecorServiceId { get; set; }
        public string DecorServiceName { get; set; } // Tên dịch vụ yêu thích
    }
}
