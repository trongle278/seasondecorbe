using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
using Repository.Interfaces;

namespace Repository.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository AccountRepository { get; }
        IProviderRepository ProviderRepository { get; }
        IRoleRepository RoleRepository { get; }
        IDecorCategoryRepository DecorCategoryRepository { get; }
        IChatRepository ChatRepository { get; }

        int Save();
        Task CommitAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
