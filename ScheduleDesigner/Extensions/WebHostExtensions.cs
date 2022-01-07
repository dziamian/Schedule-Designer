using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Extensions
{
    public static class WebHostExtensions
    {
        public static IHost UnlockAllResources(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var _unitOfWork = services.GetService<IUnitOfWork>();

                var _courseEditions = _unitOfWork.CourseEditions.GetAll();
                foreach (var courseEdition in _courseEditions)
                {
                    courseEdition.LockUserId = null;
                    courseEdition.LockUserConnectionId = null;
                }
                _courseEditions.UpdateRange(_courseEditions);

                var _schedulePositions = _unitOfWork.SchedulePositions.GetAll();
                foreach (var schedulePosition in _schedulePositions)
                {
                    schedulePosition.LockUserId = null;
                    schedulePosition.LockUserConnectionId = null;
                }
                _schedulePositions.UpdateRange(_schedulePositions);
                var result = _unitOfWork.Complete();
            }

            return host;
        }
    }
}
