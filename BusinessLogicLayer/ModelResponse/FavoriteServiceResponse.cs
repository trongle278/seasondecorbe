using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse
{
    public class FavoriteServiceResponse
    {
        public int FavoriteId { get; set; }
        public DecorServiceById DecorServiceDetails { get; set; }
    }
}
