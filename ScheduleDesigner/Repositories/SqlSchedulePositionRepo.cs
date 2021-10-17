using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class SqlSchedulePositionRepo : RepoBase<SchedulePosition>, ISchedulePositionRepo
    {
        public SqlSchedulePositionRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
