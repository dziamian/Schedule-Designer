using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class SqlScheduledMovePositionRepo : RepoBase<ScheduledMovePosition>, IScheduledMovePositionRepo
    {
        public SqlScheduledMovePositionRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
