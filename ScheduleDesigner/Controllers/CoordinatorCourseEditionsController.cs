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
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{

    [ODataRoutePrefix("CoordinatorCourseEditions")]
    public class CoordinatorCourseEditionsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public CoordinatorCourseEditionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetCoordinatorCourseEditions()
        {
            return Ok(_unitOfWork.CoordinatorCourseEditions.GetAll());
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        public IActionResult CreateCoordinatorCourseEdition([FromBody] CoordinatorCourseEditionDto coordinatorCourseEdition, [FromQuery] string connectionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (connectionId == null)
            {
                return BadRequest("Connection id not found.");
            }

            try
            {
                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);


                    var currentCourseEdition = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == coordinatorCourseEdition.CourseId 
                        && e.CourseEditionId == coordinatorCourseEdition.CourseEditionId)
                        .Include(e => e.Coordinators)
                        .FirstOrDefault();

                    if (currentCourseEdition == null)
                    {
                        return BadRequest("Course edition not found.");
                    }

                    if (currentCourseEdition.Coordinators.Any(e => e.CoordinatorId == coordinatorCourseEdition.CoordinatorId))
                    {
                        return BadRequest("This coordinator is already assigned.");
                    }

                    var coordinatorCourseEditions = _unitOfWork.CoordinatorCourseEditions
                        .Get(e => e.CoordinatorId == coordinatorCourseEdition.CoordinatorId)
                        .Include(e => e.CourseEdition)
                        .Select(e => e.CourseEdition)
                        .ToList();

                    coordinatorCourseEditions.Add(currentCourseEdition);

                    
                    var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

                    var courseEditionKeys = coordinatorCourseEditions.Select(e => new CourseEditionKey
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
                        var notLockedCurrentCourseEdition = _unitOfWork.CourseEditions
                            .Get(e => e.CourseId == coordinatorCourseEdition.CourseId && e.CourseEditionId == coordinatorCourseEdition.CourseEditionId
                                && (e.LockUserId != userId || e.LockUserConnectionId != connectionId))
                            .ToList();

                        if (notLockedCurrentCourseEdition.Any()) 
                        {
                            return BadRequest("You did not lock chosen course edition.");
                        }

                        var notLockedCourseEditions = _unitOfWork.CoordinatorCourseEditions
                            .Get(e => e.CoordinatorId == coordinatorCourseEdition.CoordinatorId 
                                && (e.CourseEdition.LockUserId != userId || e.CourseEdition.LockUserConnectionId != connectionId))
                            .Include(e => e.CourseEdition)
                            .ToList();

                        if (notLockedCourseEditions.Any())
                        {
                            return BadRequest("You did not lock all coordinator course editions.");
                        }

                        var courseEditionIds = courseEditionKeys.Select(e => e.CourseEditionId).ToList();
                        
                        var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
                        var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

                        var schedulePositionKeys = _unitOfWork.SchedulePositions
                        .Get(e => courseEditionIds.Contains(e.CourseEditionId)).Select(e => new SchedulePositionKey
                        {
                            TimestampId = e.TimestampId,
                            RoomId = e.RoomId
                        }).ToList();

                        lock (ScheduleHub.SchedulePositionLocksL1)
                        lock (ScheduleHub.SchedulePositionLocksL2)
                        {
                            ScheduleHub.AddSchedulePositionsLocksL1(schedulePositionKeys, schedulePositionQueuesL1);
                            ScheduleHub.AddSchedulePositionsLocksL2(schedulePositionKeys, schedulePositionQueuesL2);
                        }

                        ScheduleHub.EnterQueues(schedulePositionQueuesL1.Values);
                        ScheduleHub.EnterQueues(schedulePositionQueuesL2.Values);
                        try
                        {
                            var notLockedSchedulePositions = _unitOfWork.SchedulePositions
                                .Get(e => courseEditionIds.Contains(e.CourseEditionId)
                                    && (e.LockUserId != userId || e.LockUserConnectionId != connectionId))
                                .ToList();

                            if (notLockedSchedulePositions.Any())
                            {
                                return BadRequest("You did not lock all required positions in schedule.");
                            }

                            //no conflicts
                            var currentTimestamps = _unitOfWork.SchedulePositions
                                .Get(e => e.CourseEditionId == currentCourseEdition.CourseEditionId)
                                .Include(e => e.Timestamp)
                                .Select(e => e.Timestamp)
                                .ToList();

                            var otherCourseEditionIds = courseEditionIds.Where(e => e != currentCourseEdition.CourseEditionId).ToList();
                            var otherTimestamps = _unitOfWork.SchedulePositions
                                .Get(e => otherCourseEditionIds.Contains(e.CourseEditionId))
                                .Include(e => e.Timestamp)
                                .Select(e => e.Timestamp)
                                .ToList();

                            var intersectedTimestamps = currentTimestamps.Intersect(otherTimestamps).ToList();
                            if (intersectedTimestamps.Any())
                            {
                                var timestamps = string.Join(", ", intersectedTimestamps);
                                return BadRequest($"Conflict detected for Timestamps [PeriodIndex, Day, Week] {timestamps}.");
                            }

                            //add
                            var _coordinatorCourseEdition = _unitOfWork.CoordinatorCourseEditions.Add(coordinatorCourseEdition.FromDto()).Result;

                            if (_coordinatorCourseEdition != null)
                            {
                                _unitOfWork.Complete();
                                return Created(_coordinatorCourseEdition);
                            }
                            return NotFound();
                        }
                        finally
                        {
                            ScheduleHub.RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                            ScheduleHub.RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                            ScheduleHub.ExitQueues(schedulePositionQueuesL2.Values);
                            ScheduleHub.ExitQueues(schedulePositionQueuesL1.Values);
                        }
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
        [ODataRoute("({key1},{key2},{key3})")]
        public async Task<IActionResult> DeleteCoordinatorCourseEdition([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3)
        {
            try
            {
                var result = await _unitOfWork.CoordinatorCourseEditions
                    .Delete(e => e.CourseId == key1 && e.CourseEditionId == key2 && e.CoordinatorId == key3);
                if (result < 0)
                {
                    return NotFound();
                }

                await _unitOfWork.CompleteAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
