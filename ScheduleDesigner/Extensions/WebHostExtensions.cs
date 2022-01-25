using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Models;
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
                _unitOfWork.Complete();
            }

            return host;
        }

        public static IHost SetDefaultSettings(this IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                var _unitOfWork = services.GetService<IUnitOfWork>();

                var settings = _unitOfWork.Settings.GetAll().Any();
                if (settings)
                {
                    return host;
                }

                Console.WriteLine("Settings have not been specified. Trying to set default . . .");
                
                var courses = _unitOfWork.Courses.GetAll().Any();
                if (courses)
                {
                    throw new Exception("Could not set default settings because some courses exist in database. Please remove them first.");
                }

                var timestamps = _unitOfWork.Timestamps.GetAll().Any();
                if (timestamps)
                {
                    throw new Exception("Could not set default settings because some timestamps exist in database. Please remove them first.");
                }

                var defaultSettings = new Settings
                {
                    CourseDurationMinutes = 120,
                    StartTime = TimeSpan.FromHours(8),
                    EndTime = TimeSpan.FromHours(20),
                    TermDurationWeeks = 15
                };

                Methods.AddTimestamps(defaultSettings, _unitOfWork.Context.Database.GetConnectionString());
                _unitOfWork.Settings.Add(defaultSettings);

                _unitOfWork.Complete();

                Console.WriteLine("Default settings have been specified.");
            }

            return host;
        }
    }
}
