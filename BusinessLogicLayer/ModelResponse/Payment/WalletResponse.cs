using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogicLayer.ModelResponse.Payment
{
    public class WalletResponse
    {
        public int WalletId { get; set; }
        public decimal Balance { get; set; }
    }
}
