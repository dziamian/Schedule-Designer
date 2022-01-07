using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.Base
{
    public interface IRepoBase<T> where T : class
    {
        DbSet<T> GetAll();
        IQueryable<T> Get(Expression<Func<T, bool>> predicate);
        Task<T> GetFirst(Expression<Func<T, bool>> predicate);
        Task<T> Add(T entity);
        T Update(T entity);
        Task<int> Delete(Expression<Func<T, bool>> predicate);
    }
}
