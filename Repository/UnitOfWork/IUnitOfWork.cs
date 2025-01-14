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
        /*
            Khai báo interfaces cho UnitOfWork ở đây
            
            Ex:
            IAccountRepository AccountRepository { get; }
        */

        IAccountRepository AccountRepository { get; }

        int Save();
        Task CommitAsync();
        Task<IDbContextTransaction> BeginTransactionAsync();
    }
}
