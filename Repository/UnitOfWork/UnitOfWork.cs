using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;

namespace Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HomeDecorDBContext _context;

        public UnitOfWork(HomeDecorDBContext context, IConfiguration configuration)
        {
            _context = context;
            /*
                Khai báo Repository cho UnitOfWork ở đây
                
                Ex:
                AccountRepository = new AccountRepository(_context);
            */
        }

        /*
            public IAccountRepository AccountRepository { get; private set; }
        */

        public void Dispose()
        {
            _context.Dispose();
        }
        public async Task CommitAsync()
            => await _context.SaveChangesAsync();

        public async Task<IDbContextTransaction> BeginTransactionAsync() => await _context.Database.BeginTransactionAsync();

        public int Save()
        {
            return _context.SaveChanges();
        }
    }
}
