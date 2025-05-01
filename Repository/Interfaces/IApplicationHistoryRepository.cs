using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Repository.GenericRepository;
using Repository.Repositories;

namespace Repository.Interfaces
{
    public interface IApplicationHistoryRepository : IGenericRepository<ApplicationHistory>
    {
    }
}
