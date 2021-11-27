using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Extensions
{
    public static class WebHostExtensions
    {
        public static IHost UnlockAllResources(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var _courseEditionsRepo = services.GetService<ICourseEditionRepo>();
                var _schedulePositionsRepo = services.GetService<ISchedulePositionRepo>();

                var _courseEditions = _courseEditionsRepo.GetAll();
                foreach (var courseEdition in _courseEditions)
                {
                    courseEdition.LockUserId = null;
                    courseEdition.LockUserConnectionId = null;
                }
                _courseEditions.UpdateRange(_courseEditions);
                var result1 = _courseEditionsRepo.SaveChanges().Result;

                var _schedulePositions = _schedulePositionsRepo.GetAll();
                foreach (var schedulePosition in _schedulePositions)
                {
                    schedulePosition.LockUserId = null;
                    schedulePosition.LockUserConnectionId = null;
                }
                _schedulePositions.UpdateRange(_schedulePositions);
                var result2 = _schedulePositionsRepo.SaveChanges().Result;
            }

            return host;
        }
    }
}
