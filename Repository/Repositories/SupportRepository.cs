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
    public class SupportRepository : GenericRepository<Support>, ISupportRepository
    {
        public SupportRepository(HomeDecorDBContext context) : base(context)
        {
        }
    }
}
