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
using static ScheduleDesigner.Helpers;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("SchedulePositions")]
    public class SchedulePositionsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public SchedulePositionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetSchedulePositions()
        {
            return Ok(_unitOfWork.SchedulePositions.GetAll());
        }

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

        /*private static readonly ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>
            CourseEditionLocks = new ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>
            SchedulePositionLocks = new ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>
            CoordinatorPositionLocks = new ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>
            GroupPositionLocks = new ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>();*/

        /*private void RemoveLocks(
            ConcurrentQueue<object> courseEditionQueue, 
            CourseEditionKey courseEditionKey, 
            List<ConcurrentQueue<object>> schedulePositionQueues,
            List<SchedulePositionKey> schedulePositionKeys,
            List<ConcurrentQueue<object>> coordinatorPositionQueues,
            List<CoordinatorPositionKey> coordinatorPositionKeys,
            List<ConcurrentQueue<object>> groupPositionQueues,
            List<GroupPositionKey> groupPositionKeys)
        {
            courseEditionQueue.TryDequeue(out _);
            if (courseEditionQueue.IsEmpty)
            {
                CourseEditionLocks.TryRemove(courseEditionKey, out _);
            }
            for (var i = 0; i < schedulePositionQueues.Count; ++i)
            {
                schedulePositionQueues[i].TryDequeue(out _);
                if (schedulePositionQueues[i].IsEmpty)
                {
                    SchedulePositionLocks.TryRemove(schedulePositionKeys[i], out _);
                }
            }
            for (var i = 0; i < coordinatorPositionQueues.Count; ++i)
            {
                coordinatorPositionQueues[i].TryDequeue(out _);
                if (coordinatorPositionQueues[i].IsEmpty)
                {
                    CoordinatorPositionLocks.TryRemove(coordinatorPositionKeys[i], out _);
                }
            }
            for (var i = 0; i < groupPositionQueues.Count; ++i)
            {
                groupPositionQueues[i].TryDequeue(out _);
                if (groupPositionQueues[i].IsEmpty)
                {
                    GroupPositionLocks.TryRemove(groupPositionKeys[i], out _);
                }
            }
        }*/

        /*[Authorize(Policy = "Designer")]
        [HttpPost]
        public IActionResult AddSchedulePositions(ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var courseId = (int) parameters["CourseId"];
            var courseEditionId = (int) parameters["CourseEditionId"];
            var roomId = (int) parameters["RoomId"];
            var periodIndex = (int) parameters["PeriodIndex"];
            var day = (int) parameters["Day"];
            var weeks = (parameters["Weeks"] as IEnumerable<int> ?? new int[]{}).ToArray();

            CourseEditionKey courseEditionKey = null;
            var schedulePositionKeys = new List<SchedulePositionKey>();
            var coordinatorPositionKeys = new List<CoordinatorPositionKey>();
            var groupPositionKeys = new List<GroupPositionKey>();
            ConcurrentQueue<object> courseEditionQueue = null;
            var schedulePositionQueues = new List<ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new List<ConcurrentQueue<object>>();
            var groupPositionQueues = new List<ConcurrentQueue<object>>();

            var courseEnqueued = false;
            Array.Sort(weeks);

            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());
                courseEditionQueue.Enqueue(new object());

                courseEnqueued = true;

                lock (SchedulePositionLocks)
                {
                    foreach (var week in weeks)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, PeriodIndex = periodIndex, Day = day, Week = week };
                        schedulePositionKeys.Add(key);
                        var queue = SchedulePositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                        schedulePositionQueues.Add(queue);
                        queue.Enqueue(new object());
                    }
                }

                Monitor.Enter(courseEditionQueue);
                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    var _courseEdition = _courseEditionRepo
                        .Get(e => e.CourseId == courseId && e.CourseEditionId == courseEditionId)
                        .Include(e => e.Coordinators)
                        .Include(e => e.Groups)
                            .ThenInclude(e => e.Group)
                        .Include(e => e.SchedulePositions)
                        .Include(e => e.Course)
                            .ThenInclude(e => e.Rooms);

                    if (!_courseEdition.Any())
                    {
                        RemoveLocks(
                            courseEditionQueue,
                            courseEditionKey,
                            schedulePositionQueues,
                            schedulePositionKeys,
                            coordinatorPositionQueues,
                            coordinatorPositionKeys,
                            groupPositionQueues,
                            groupPositionKeys
                        );

                        return NotFound();
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition.LockUserId != userId)//|| courseEdition.LockUserConnectionId != Context.ConnectionId)
                    {
                        RemoveLocks(
                            courseEditionQueue,
                            courseEditionKey,
                            schedulePositionQueues,
                            schedulePositionKeys,
                            coordinatorPositionQueues,
                            coordinatorPositionKeys,
                            groupPositionQueues,
                            groupPositionKeys
                        );

                        return BadRequest("You didn't lock this course edition.");
                    }

                    var _settings = _settingsRepo.GetSettings().Result;
                    if (_settings == null)
                    {
                        RemoveLocks(
                            courseEditionQueue,
                            courseEditionKey,
                            schedulePositionQueues,
                            schedulePositionKeys,
                            coordinatorPositionQueues,
                            coordinatorPositionKeys,
                            groupPositionQueues,
                            groupPositionKeys
                        );

                        return BadRequest("Application settings has not been specified.");
                    }

                    if (!courseEdition.Course.Rooms.Select(e => e.RoomId).Contains(roomId))
                    {
                        RemoveLocks(
                            courseEditionQueue,
                            courseEditionKey,
                            schedulePositionQueues,
                            schedulePositionKeys,
                            coordinatorPositionQueues,
                            coordinatorPositionKeys,
                            groupPositionQueues,
                            groupPositionKeys
                        );

                        return BadRequest("Chosen room does not exist or has not been assigned to this course.");
                    }

                    var courseDurationMinutes = _settings.CourseDurationMinutes;
                    var totalMinutes = weeks.Length * courseDurationMinutes;
                    if (courseEdition.Course.UnitsMinutes -
                        courseEdition.SchedulePositions.Count * courseDurationMinutes < totalMinutes)
                    {
                        RemoveLocks(
                            courseEditionQueue,
                            courseEditionKey,
                            schedulePositionQueues,
                            schedulePositionKeys,
                            coordinatorPositionQueues,
                            coordinatorPositionKeys,
                            groupPositionQueues,
                            groupPositionKeys
                        );

                        return BadRequest("You cannot add any more units of this course to the schedule.");
                    }

                    var coordinatorsIds = courseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                    var groupsIds = CourseEditionsController.GetNestedGroupsIds(courseEdition, _groupRepo).ToArray();
                    Array.Sort(coordinatorsIds);
                    Array.Sort(groupsIds);

                    lock (CoordinatorPositionLocks)
                    lock (GroupPositionLocks)
                    {
                        foreach (var week in weeks)
                        {
                            foreach (var coordinatorId in coordinatorsIds)
                            {
                                var key = new CoordinatorPositionKey
                                    {CoordinatorId = coordinatorId, PeriodIndex = periodIndex, Day = day, Week = week};
                                coordinatorPositionKeys.Add(key);
                                var queue = CoordinatorPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                coordinatorPositionQueues.Add(queue);
                                queue.Enqueue(new object());
                            }

                            foreach (var groupId in groupsIds)
                            {
                                var key = new GroupPositionKey
                                    { GroupId = groupId, PeriodIndex = periodIndex, Day = day, Week = week };
                                groupPositionKeys.Add(key);
                                var queue = GroupPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                groupPositionQueues.Add(queue);
                                queue.Enqueue(new object());
                            }
                        }
                    }

                    foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                    {
                        Monitor.Enter(coordinatorPositionQueue);
                    }
                    foreach (var groupPositionQueue in groupPositionQueues)
                    {
                        Monitor.Enter(groupPositionQueue);
                    }

                    try
                    {
                        var _timestamps = _timestampRepo
                            .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e).ToList();

                        var _schedulePositions = _schedulePositionRepo
                            .Get(e => _timestamps.Contains(e.TimestampId)
                                      && (e.RoomId == roomId || e.CourseEdition.Coordinators
                                                                 .Select(e => e.CoordinatorId)
                                                                 .Any(e => coordinatorsIds.Contains(e))
                                                             || e.CourseEdition.Groups.Select(e => e.GroupId)
                                                                 .Any(e => groupsIds.Contains(e))))
                            .Include(e => e.CourseEdition)
                                .ThenInclude(e => e.Coordinators)
                            .Include(e => e.CourseEdition)
                                .ThenInclude(e => e.Groups)
                            .Select(e => new {e.TimestampId, e.RoomId});

                        if (_schedulePositions.Any())
                        {
                            RemoveLocks(
                                courseEditionQueue,
                                courseEditionKey,
                                schedulePositionQueues,
                                schedulePositionKeys,
                                coordinatorPositionQueues,
                                coordinatorPositionKeys,
                                groupPositionQueues,
                                groupPositionKeys
                            );

                            return BadRequest("Some conflicts with other courses occurred.");
                        }

                        var _courseRoomTimestamps = _courseRoomTimestampRepo
                            .Get(e => e.RoomId == roomId && _timestamps.Contains(e.TimestampId) &&
                                      e.CourseId == courseEdition.CourseId)
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e);

                        var schedulePositions = _timestamps.Select(timestampId => new SchedulePosition
                            {
                                RoomId = roomId,
                                TimestampId = timestampId,
                                CourseId = courseEdition.CourseId,
                                CourseEditionId = courseEdition.CourseEditionId,
                                CourseRoomTimestamp = !_courseRoomTimestamps.Contains(timestampId) ? new CourseRoomTimestamp
                                {
                                    RoomId = roomId, 
                                    TimestampId = timestampId, 
                                    CourseId = courseEdition.CourseId
                                } : null
                            }).ToList();

                        _schedulePositionRepo.GetAll().AddRange(schedulePositions);
                        
                        var result1 = _schedulePositionRepo.SaveChanges().Result;
                        //var result2 = Clients.All.blabla
                    }
                    finally
                    {
                        foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                        {
                            Monitor.Exit(coordinatorPositionQueue);
                        }
                        foreach (var groupPositionQueue in groupPositionQueues)
                        {
                            Monitor.Exit(groupPositionQueue);
                        }
                    }

                    RemoveLocks(
                        courseEditionQueue,
                        courseEditionKey,
                        schedulePositionQueues,
                        schedulePositionKeys,
                        coordinatorPositionQueues,
                        coordinatorPositionKeys,
                        groupPositionQueues,
                        groupPositionKeys
                    );

                    return Ok();
                }
                finally
                {
                    Monitor.Exit(courseEditionQueue);
                    foreach (var schedulePositionQueue in schedulePositionQueues)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                if (!courseEnqueued)
                {
                    return BadRequest(e.Message);
                }

                Monitor.Enter(courseEditionQueue);
                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    RemoveLocks(
                        courseEditionQueue,
                        courseEditionKey,
                        schedulePositionQueues,
                        schedulePositionKeys,
                        coordinatorPositionQueues,
                        coordinatorPositionKeys,
                        groupPositionQueues,
                        groupPositionKeys
                    );
                }
                finally
                {
                    Monitor.Exit(courseEditionQueue);
                    foreach (var schedulePositionQueue in schedulePositionQueues)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }

                return BadRequest(e.Message);
            }
        }*/
    }
}
