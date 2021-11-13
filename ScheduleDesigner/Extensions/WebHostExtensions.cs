using System;
using System.Collections.Generic;
using System.Linq;
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

                var _courseEditions = _courseEditionsRepo.GetAll();
                foreach (var courseEdition in _courseEditions)
                {
                    courseEdition.LockUserId = null;
                    courseEdition.LockUserConnectionId = null;
                }
                _courseEditions.UpdateRange(_courseEditions);
                var result = _courseEditionsRepo.SaveChanges().Result;
            }

            return host;
        }
    }
}
