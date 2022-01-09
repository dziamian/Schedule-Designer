using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using System;

namespace ScheduleDesigner.Repositories.Interfaces
{
    public interface IScheduledMoveRepo : IRepoBase<ScheduledMove>
    {
        public int DeleteMany(Func<ScheduledMove, bool> predicate);
    }
}
