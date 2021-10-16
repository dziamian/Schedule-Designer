using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.Base
{
    public abstract class RepoBase<T> : IRepoBase<T> where T : class
    {
        protected ScheduleDesignerDbContext _context { get; set; }

        public RepoBase(ScheduleDesignerDbContext context)
        {
            _context = context;
        }

        public DbSet<T> GetAll()
        {
            if (_context == null)
            {
                return null;
            }

            return _context.Set<T>();
        }
        public IQueryable<T> Get(Expression<Func<T, bool>> predicate)
        {
            if (_context == null)
            {
                return null;
            }

            return _context.Set<T>()
                .Where(predicate);
        }
        public async Task<T> GetFirst(Expression<Func<T, bool>> predicate)
        {
            if (_context == null)
            {
                return null;
            }

            return await _context.Set<T>()
                .FirstOrDefaultAsync(predicate);
        }
        public async Task<T> Add(T entity)
        {
            if (_context == null)
            {
                return null;
            }

            var result = await _context.Set<T>().AddAsync(entity);

            await _context.SaveChangesAsync();

            return result.Entity;
        }

        public async Task<T> Update(T entity)
        {
            if (_context == null)
            {
                return null;
            }

            _context.Entry(entity).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return entity;
        }

        public async Task<int> Delete(Expression<Func<T, bool>> predicate)
        {
            if (_context == null)
            {
                return -1;
            }

            var result = await _context.Set<T>()
                .FirstOrDefaultAsync(predicate);

            if (result == null)
            {
                return -1;
            }

            _context.Set<T>().Remove(result);
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SaveChanges()
        {
            if (_context == null)
            {
                return -1;
            }

            return await _context.SaveChangesAsync();
        }
    }
}
