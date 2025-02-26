using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;

namespace BusinessLogicLayer.Interfaces
{
    public interface IElasticClientService
    {
        Task IndexDecorServiceAsync(DecorService decorService);
        Task DeleteDecorServiceAsync(int id);
        Task<List<DecorService>> SearchDecorServicesAsync(string keyword);
    }
}
