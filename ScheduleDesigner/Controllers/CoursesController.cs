using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Courses")]
    public class CoursesController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public CoursesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        public IActionResult CreateCourse([FromBody] CourseDto courseDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
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
                        return BadRequest("Application settings has not been specified.");
                    }

                    if (!Methods.AreUnitsMinutesValid(courseDto.UnitsMinutes, _unitOfWork.Settings.GetFirst(e => true).Result))
                    {
                        ModelState.AddModelError("CourseUnitsMinutes", "Could not calculate the valid amount of courses in term.");
                        return BadRequest(ModelState);
                    }

                    var _course = _unitOfWork.Courses.Add(courseDto.FromDto()).Result;

                    if (_course != null)
                    {
                        _unitOfWork.Complete();
                        return Created(_course);
                    }
                    return NotFound();
                }
                finally
                {
                    Monitor.Exit(SchedulePositionsController.ScheduleLock);
                }
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetCourses()
        {
            return Ok(_unitOfWork.Courses.GetAll());
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key1})")]
        public IActionResult GetCourse([FromODataUri] int key1)
        {
            try
            {
                var _course = _unitOfWork.Courses.Get(e => e.CourseId == key1);
                if (!_course.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_course));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("({key1})")]
        public IActionResult UpdateCourse([FromODataUri] int key1, [FromBody] Delta<Course> delta, [FromQuery] string connectionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var settings = _unitOfWork.Settings.GetFirst(e => true).Result;

            if (settings == null)
            {
                return BadRequest("Application settings are not specified.");
            }

            try
            {
                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var _course = _unitOfWork.Courses.GetFirst(e => e.CourseId == key1).Result;
                    if (_course == null)
                    {
                        return NotFound();
                    }

                    if (!delta.GetChangedPropertyNames().Contains("UnitsMinutes"))
                    {
                        delta.Patch(_course);

                        _unitOfWork.Complete();

                        return Ok(_course);
                    }

                    var _settings = _unitOfWork.Settings.GetFirst(e => true).Result;
                    if (!Methods.AreUnitsMinutesValid(_course.UnitsMinutes, _settings))
                    {
                        ModelState.AddModelError("CourseUnitsMinutes", "Couldn't calculate the valid amount of courses in term.");
                        return BadRequest(ModelState);
                    }

                    var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);
                    if (connectionId == null)
                    {
                        return BadRequest("Connection id not found.");
                    }
                    
                    var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

                    var courseEditionKeys = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == key1).Select(e => new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }).ToList();

                    lock (ScheduleHub.CourseEditionLocks)
                    {
                        ScheduleHub.AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                    }

                    ScheduleHub.EnterQueues(courseEditionQueues.Values);
                    try
                    {
                        var notLockedCourseEditions = _unitOfWork.CourseEditions
                            .Get(e => e.CourseId == key1 
                                && (e.LockUserId != userId || e.LockUserConnectionId != connectionId));

                        if (notLockedCourseEditions.Any())
                        {
                            return BadRequest("You did not lock all editions for chosen course.");
                        }
                        
                        delta.TryGetPropertyValue("UnitsMinutes", out var unitsMinutesObject);
                        var unitsMinutes = (int)unitsMinutesObject;

                        if (_course.UnitsMinutes > unitsMinutes) 
                        {
                            var schedulePositionCounts = _unitOfWork.SchedulePositions
                                .Get(e => e.CourseId == key1)
                                .GroupBy(e => e.CourseEditionId)
                                .Select(e => new { e.Key, Count = e.Count() })
                                .ToList();

                            var maxCourseUnits = (int)Math.Ceiling(unitsMinutes / (settings.CourseDurationMinutes * 1.0));
                            if (schedulePositionCounts.Any(e => e.Count > maxCourseUnits))
                            {
                                return BadRequest("There is already too many units of some course edition in the schedule.");
                            }
                        }

                        delta.Patch(_course);

                        _unitOfWork.Complete();

                        return Ok(_course);
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
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete]
        [ODataRoute("({key1})")]
        public async Task<IActionResult> DeleteCourse([FromODataUri] int key1)
        {
            try
            {
                var result = await _unitOfWork.Courses.Delete(e => e.CourseId == key1);
                if (result < 0)
                {
                    return NotFound();
                }

                var schedulePosition = await _unitOfWork.SchedulePositions
                    .Get(e => e.CourseId == key1).FirstOrDefaultAsync();

                if (schedulePosition != null)
                {
                    return BadRequest("You cannot remove this course because it contains some positions in schedule.");
                }

                await _unitOfWork.CompleteAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        public IActionResult ClearCourses()
        {
            try
            {
                var schedulePositions = _unitOfWork.SchedulePositions.GetAll();
                if (schedulePositions.Any())
                {
                    return BadRequest("You cannot clear courses because there are some positions in schedule assigned to them.");
                }

                int courseRoomsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [CourseRooms]");
                int coursesAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [Courses]");

                return Ok(new { CourseRoomsAffected = courseRoomsAffected, CoursesAffected = coursesAffected });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
