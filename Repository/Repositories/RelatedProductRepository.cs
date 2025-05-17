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
    public class RelatedProductRepository : GenericRepository<RelatedProduct>, IRelatedProductRepository
    {
        public RelatedProductRepository(HomeDecorDBContext context) : base(context)
        {
        }
    }
}
