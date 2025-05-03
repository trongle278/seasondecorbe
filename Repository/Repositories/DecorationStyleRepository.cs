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
    public class DecorationStyleRepository: GenericRepository<DecorationStyle>, IDecorationStyleRepository
    {
        public DecorationStyleRepository(HomeDecorDBContext context) : base(context)
        {
        }
    }
}
