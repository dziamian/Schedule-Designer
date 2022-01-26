using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Helpers;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("SchedulePositions")]
    public class SchedulePositionsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public static readonly object ScheduleLock = new object();
        public static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);

        public SchedulePositionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetSchedulePositions()
        {
            return Ok(_unitOfWork.SchedulePositions.GetAll());
        }
        
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("Service.GetSchedulePositions(RoomId={RoomId},PeriodIndex={PeriodIndex},Day={Day},Weeks={Weeks})")]
        public IActionResult GetSchedulePositions([FromODataUri] int RoomId, [FromODataUri] int PeriodIndex, [FromODataUri] int Day, [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _schedulePositions = _unitOfWork.SchedulePositions
                    .Get(e => e.RoomId == RoomId && e.Timestamp.PeriodIndex == PeriodIndex
                                                 && e.Timestamp.Day == Day &&
                                                 Weeks.Contains(e.Timestamp.Week))
                    .Include(e => e.Timestamp);

                return Ok(_schedulePositions);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetScheduleAmount([FromODataUri] IEnumerable<int> CourseEditionIds)
        {
            try
            {
                if (CourseEditionIds.Count() == 0)
                {
                    return BadRequest();
                }

                var _schedulePositions = _unitOfWork.SchedulePositions
                    .Get(e => CourseEditionIds.Contains(e.CourseEditionId))
                    .GroupBy(e => e.CourseEditionId)
                    .Select(e => new ScheduleAmount { CourseEditionId = e.Key, Count = e.Count() })
                    .ToList();

                return Ok(_schedulePositions);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery(MaxExpansionDepth = 3)]
        public IActionResult GetFilteredSchedule(
            [FromODataUri] IEnumerable<int> CoordinatorsIds, 
            [FromODataUri] IEnumerable<int> GroupsIds, 
            [FromODataUri] IEnumerable<int> RoomsIds, 
            [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var coordinatorsCount = CoordinatorsIds.Count();
                var groupsCount = GroupsIds.Count();

                var coordinatorCourseEditionIds = new List<int>();
                if (coordinatorsCount > 0)
                {
                    coordinatorCourseEditionIds = _unitOfWork.CoordinatorCourseEditions
                        .GetAll()
                        .Where(e => CoordinatorsIds.Contains(e.CoordinatorId))
                        .Select(e => e.CourseEditionId).ToList();
                }

                var groupCourseEditionIds = new List<int>();
                if (groupsCount > 0)
                {
                    groupCourseEditionIds = _unitOfWork.GroupCourseEditions
                        .GetAll()
                        .Where(e => GroupsIds.Contains(e.GroupId))
                        .Select(e => e.CourseEditionId)
                        .ToList();
                }
                var courseEditionIds = coordinatorCourseEditionIds.Union(groupCourseEditionIds);

                var predicate = PredicateBuilder.New<SchedulePosition>(false);
                predicate = predicate
                    .Or(e => courseEditionIds.Contains(e.CourseEditionId));

                if (RoomsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => RoomsIds.Contains(e.RoomId));
                }

                var finalPredicate = predicate.And(e => Weeks.Contains(e.Timestamp.Week));


                var _schedulePositions = _unitOfWork.SchedulePositions
                    .Get(finalPredicate)
                    .Include(e => e.Timestamp);

                return Ok(_schedulePositions);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("Service.GetRoomsAvailability(RoomsIds={RoomsIds},PeriodIndex={PeriodIndex},Day={Day},Weeks={Weeks})")]
        public IActionResult GetRoomsAvailibility(
            [FromODataUri] IEnumerable<int> RoomsIds, 
            [FromODataUri] int PeriodIndex, 
            [FromODataUri] int Day, 
            [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _rooms = _unitOfWork.Rooms
                    .Get(e => RoomsIds.Contains(e.RoomId))
                    .Select(e => e.RoomId);

                var rooms = new Dictionary<int, RoomAvailability>();
                foreach (var _room in _rooms)
                {
                    rooms.TryAdd(_room, new RoomAvailability {RoomId = _room, IsBusy = false});
                }

                var _schedulePositions = _unitOfWork.SchedulePositions
                    .Get(e => _rooms.Contains(e.RoomId) && e.Timestamp.PeriodIndex == PeriodIndex 
                        && e.Timestamp.Day == Day && Weeks.Contains(e.Timestamp.Week))
                    .Include(e => e.Timestamp)
                    .GroupBy(e => e.RoomId)
                    .Select(e => new RoomAvailability {RoomId = e.Key});

                foreach (var schedulePosition in _schedulePositions)
                {
                    var updatedRoom = rooms[schedulePosition.RoomId];
                    updatedRoom.IsBusy = true;
                    rooms[schedulePosition.RoomId] = updatedRoom;
                }

                return Ok(rooms.Values);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        public IActionResult ClearSchedule()
        {
            try
            {
                int messagesAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [Messages]");
                int scheduledMovePositionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [ScheduledMovePositions]");
                int scheduledMovesAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [ScheduledMoves]");
                int schedulePositionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [SchedulePositions]");

                return Ok(new 
                {
                    MessagesAffected = messagesAffected,
                    ScheduledMovePositionsAffected = scheduledMovePositionsAffected,
                    ScheduledMovesAffected = scheduledMovesAffected,
                    SchedulePositionsAffected = schedulePositionsAffected
                });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
