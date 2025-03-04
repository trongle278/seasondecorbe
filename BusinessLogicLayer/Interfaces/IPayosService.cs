using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLogicLayer.ModelRequest;
using BusinessLogicLayer.ModelResponse;
using Net.payOS.Types;

namespace BusinessLogicLayer.Interfaces
{
    public interface IPayosService
    {
        Task<CreatePaymentResult> CreatePaymentLinkAsync(long orderCode, int amount, string description, List<ItemData> items = null);
    }
}
