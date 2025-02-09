using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Repository.GenericRepository;
using Repository.Interfaces;

namespace Repository
{
    public class DecoratorRepository : GenericRepository<Decorator>, IDecoratorRepository
    {
        public DecoratorRepository(HomeDecorDBContext context) : base(context)
        {
        }
    }
}
