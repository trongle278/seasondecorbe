using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Repository.GenericRepository
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, object>>[] includeProperties);
        Task<IEnumerable<T>> GetAllAsync(int limit, Expression<Func<T, bool>> filter = null, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null, Expression<Func<T, object>>[] includeProperties = null);
        Task<T> GetByIdAsync(object id);
        Task InsertAsync(T entity);
        void Update(T entity);
        void Delete(object id);
        void RemoveRange(IEnumerable<T> entities);
        void UpdateAndRelationStatus(T entity, Expression<Func<T, object>>[]? includeProperties = null);
        Task SaveAsync();
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAndFilteredAsync(
           Expression<Func<T, bool>> filter,
           int pageIndex,
           int pageSize,
           Expression<Func<T, object>> orderByExpression = null,
           bool descending = false,
           Expression<Func<T, object>>[]? includeProperties = null);
        IQueryable<T> Query(Expression<Func<T, bool>> filter);
    }
}
