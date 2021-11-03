using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Hubs.Interfaces
{
    public interface IScheduleClient
    {
        Task LockCourseEdition(int courseId, int courseEditionId);
        Task UnlockCourseEdition(int courseId, int courseEditionId);
    }
}
