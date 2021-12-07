using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
