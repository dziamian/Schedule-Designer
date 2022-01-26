using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Settings")]
    public class SettingsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public SettingsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private static bool IsDataValid(Settings settings)
        {
            return (settings.EndTime - settings.StartTime).TotalMinutes % settings.CourseDurationMinutes == 0;
        }

        private static int GetNumberOfPeriods(Settings settings)
        {
            return (int)(settings.EndTime - settings.StartTime).TotalMinutes / settings.CourseDurationMinutes;
        }
        
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var _settings = await _unitOfWork.Settings.GetFirst(e => true);
                if (_settings == null)
                {
                    return NotFound();
                }

                return Ok(_settings);
            } 
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
        
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetPeriods()
        {
            try
            {
                var _settings = await _unitOfWork.Settings.GetFirst(e => true);
                if (_settings == null)
                {
                    return NotFound();
                }

                var numberOfPeriods = GetNumberOfPeriods(_settings) + 1;
                var currentPeriod = _settings.StartTime;
                var periodsStrings = new string[numberOfPeriods];
                var courseDuration = new TimeSpan(0, _settings.CourseDurationMinutes, 0);

                periodsStrings[0] = currentPeriod.ToString(@"hh\:mm");
                for (int i = 1; i < numberOfPeriods; ++i)
                {
                    currentPeriod = currentPeriod.Add(courseDuration);
                    periodsStrings[i] = currentPeriod.ToString(@"hh\:mm");
                }

                return Ok(periodsStrings);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("")]
        public IActionResult UpdateSettings([FromBody] Delta<Settings> delta, [FromQuery] string connectionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (connectionId == null)
            {
                return BadRequest("Could not find connection id.");
            }

            try
            {
                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var _settings = _unitOfWork.Settings.GetFirst(e => true).Result;
                    if (_settings == null)
                    {
                        return NotFound();
                    }

                    delta.Patch(_settings);

                    if (!IsDataValid(_settings))
                    {
                        ModelState.AddModelError("CoursesAmount", "Couldn't calculate the valid amount of max courses per day.");
                        return BadRequest(ModelState);
                    }

                    if (delta.GetChangedPropertyNames().Contains("CourseDurationMinutes"))
                    {
                        var courses = _unitOfWork.Courses.GetAll().FirstOrDefault();

                        if (courses != null)
                        {
                            return BadRequest("Courses must be empty in order to change their durations.");
                        }
                    }

                    var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                    var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

                    var courseEditions = _unitOfWork.CourseEditions.GetAll()
                        .ToList();

                    var courseEditionKeys = courseEditions.Select(e => new CourseEditionKey
                    {
                        CourseId = e.CourseId,
                        CourseEditionId = e.CourseEditionId
                    }).ToList();

                    lock (ScheduleHub.CourseEditionLocks)
                    {
                        ScheduleHub.AddCourseEditionsLocks(courseEditionKeys, ref courseEditionQueues);
                    }

                    ScheduleHub.EnterQueues(courseEditionQueues.Values);
                    try
                    {
                        var schedulePositions = _unitOfWork.SchedulePositions.GetAll().FirstOrDefault();

                        if (schedulePositions != null)
                        {
                            return BadRequest("Schedule must be empty in order to change settings.");
                        }

                        var notLockedCourseEditions = _unitOfWork.CourseEditions
                            .Get(e => e.LockUserId != userId || e.LockUserConnectionId != connectionId);

                        if (notLockedCourseEditions.Any())
                        {
                            return BadRequest("You did not lock all course editions.");
                        }

                        Methods.RemoveTimestamps(_unitOfWork);
                        Methods.AddTimestamps(_settings, _unitOfWork.Context.Database.GetConnectionString());

                        _unitOfWork.Complete();

                        return Ok(_settings);
                    }
                    finally
                    {
                        ScheduleHub.RemoveCourseEditionsLocks(courseEditionQueues);
                        ScheduleHub.ExitQueues(courseEditionQueues.Values);
                    }
                }
                finally
                {
                    Monitor.Exit(SchedulePositionsController.ScheduleLock);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
