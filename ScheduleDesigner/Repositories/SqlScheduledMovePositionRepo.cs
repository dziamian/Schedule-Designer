using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Linq;

namespace ScheduleDesigner.Repositories
{
    public class SqlScheduledMovePositionRepo : RepoBase<ScheduledMovePosition>, IScheduledMovePositionRepo
    {
        public SqlScheduledMovePositionRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }

        public int DeleteMany(Func<ScheduledMovePosition, bool> predicate)
        {
            if (_context == null)
            {
                return -1;
            }

            var results = _context.Set<ScheduledMovePosition>()
                .Where(predicate);

            if (!results.Any())
            {
                return -1;
            }

            _context.Set<ScheduledMovePosition>().RemoveRange(results);
            return 1;
        }
    }
}
