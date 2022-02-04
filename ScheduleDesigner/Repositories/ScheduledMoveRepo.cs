using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Linq;

namespace ScheduleDesigner.Repositories
{
    public class ScheduledMoveRepo : RepoBase<ScheduledMove>, IScheduledMoveRepo
    {
        public ScheduledMoveRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }

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
