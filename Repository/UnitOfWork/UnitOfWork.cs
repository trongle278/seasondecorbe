using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Repository.Interfaces;

namespace Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly HomeDecorDBContext _context;

        public UnitOfWork(HomeDecorDBContext context, IConfiguration configuration)
        {
            _context = context;
            AccountRepository = new AccountRepository(_context);
            RoleRepository = new RoleRepository(_context);
            DecorCategoryRepository = new DecorCategoryRepository(_context);
            ChatRepository = new ChatRepository(_context);
            ProviderRepository = new ProviderRepository(_context);
            CartRepository = new CartRepository(_context);
        }

        public IAccountRepository AccountRepository { get; private set; }
        public IProviderRepository ProviderRepository { get; private set; }
        public IRoleRepository RoleRepository { get; private set; }
        public IDecorCategoryRepository DecorCategoryRepository { get; private set; }
        public IChatRepository ChatRepository { get; private set; }
        public ICartRepository CartRepository { get; private set; }

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
