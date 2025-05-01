using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Repository.GenericRepository;
using Repository.Interfaces;

namespace Repository.Repositories
{
    public class ApplicationHistoryRepository : GenericRepository<ApplicationHistory>, IApplicationHistoryRepository
    {
        public ApplicationHistoryRepository(HomeDecorDBContext context) : base(context)
        {
        }
    }
}
