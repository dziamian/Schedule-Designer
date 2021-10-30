using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Hubs.Interfaces
{
    public interface IScheduleClient
    {
        void UserUpdate();
        void UserRemove();
    }
}
