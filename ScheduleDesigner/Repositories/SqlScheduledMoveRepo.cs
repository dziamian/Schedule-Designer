using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class SqlScheduledMoveRepo : RepoBase<ScheduledMove>, IScheduledMoveRepo
    {
        public SqlScheduledMoveRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }

        public int GetNextId()
        {
            return _context.GetNextScheduledMoveId();
        }

        public int AcceptMany(Func<ScheduledMove, bool> predicate)
        {
            if (_context == null)
            {
                return -1;
            }

            var results = _context.Set<ScheduledMove>()
                .Where(predicate);

            if (!results.Any())
            {
                return -1;
            }

            foreach (var result in results)
            {
                result.IsConfirmed = true;
            }

            _context.Set<ScheduledMove>().UpdateRange(results);
            return 1;
        }

        public int DeleteMany(Func<ScheduledMove, bool> predicate)
        {
            if (_context == null)
            {
                return -1;
            }

            var results = _context.Set<ScheduledMove>()
                .Where(predicate);

            if (!results.Any())
            {
                return -1;
            }

            _context.Set<ScheduledMove>().RemoveRange(results);
            return 1;
        }
    }
}
