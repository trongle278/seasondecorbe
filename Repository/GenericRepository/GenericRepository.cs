using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using DataAccessObject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Repository.GenericRepository
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly HomeDecorDBContext _context;
        private IDbContextTransaction _transaction;
        public GenericRepository(HomeDecorDBContext context)
        {
            _context = context;
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().ToListAsync();
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, object>>[]? includeProperties = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }

            return await query.ToListAsync();
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync(int limit, Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, Expression<Func<T, object>>[]? includeProperties = null)
        {
            IQueryable<T> query = _context.Set<T>();
            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }
            if (orderBy != null)
            {
                query = orderBy(query);
            }
            return await query.Take(limit).ToListAsync();
        }

        public virtual async Task<T> GetByIdAsync(object id)
        {
            return await _context.Set<T>().FindAsync(id);
        }

        public virtual async Task InsertAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }

        public virtual void Update(T entity)
        {
            _context.Set<T>().Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public virtual void Delete(object id)
        {
            T entityToDelete = _context.Set<T>().Find(id);
            Delete(entityToDelete);
        }


        protected virtual void Delete(T entityToDelete)
        {
            if (_context.Entry(entityToDelete).State == EntityState.Detached)
            {
                _context.Set<T>().Attach(entityToDelete);
            }
            _context.Set<T>().Remove(entityToDelete);
        }

        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            _context.Set<T>().RemoveRange(entities);
        }

        public virtual async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }

        public IQueryable<T> Query(Expression<Func<T, bool>> filter)
        {
            return _context.Set<T>().Where(filter);
        }

        public virtual void UpdateAndRelationStatus(T entity, Expression<Func<T, object>>[]? includeProperties = null)
        {
            var statusProperty = typeof(T).GetProperty("Status");
            _context.Set<T>().Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
            if (includeProperties != null)
            {
                foreach (var includePropertyExpression in includeProperties)
                {
                    if (includePropertyExpression.Body is MemberExpression memberExpression)
                    {
                        string propertyName = memberExpression.Member.Name;
                        var relatedEntities = _context.Entry(entity).Collection(propertyName).Query().OfType<object>();
                        foreach (var relatedEntity in relatedEntities)
                        {
                            var relatedStatusProperty = relatedEntity.GetType().GetProperty("Status");
                            if (relatedStatusProperty != null)
                            {
                                relatedStatusProperty.SetValue(relatedEntity, statusProperty.GetValue(entity));
                            }
                        }
                    }
                }
            }
        }

        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAndFilteredAsync(
        Expression<Func<T, bool>> filter,
        int pageIndex,
        int pageSize,
        Expression<Func<T, object>> orderByExpression = null,
        bool descending = false,
        Expression<Func<T, object>>[]? includeProperties = null)
        {
            IQueryable<T> query = _context.Set<T>();

            if (filter != null && _context.Set<T>().Any(filter))
            {
                query = query.Where(filter);
            }

            if (includeProperties != null)
            {
                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }
            }

            if (orderByExpression != null)
            {
                if (descending)
                {
                    query = query.OrderByDescending(orderByExpression);
                }
                else
                {
                    query = query.OrderBy(orderByExpression);
                }
            }

            int totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }
    }
}
