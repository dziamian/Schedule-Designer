using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("SchedulePositions")]
    public class SchedulePositionsController : ODataController
    {
        private readonly ISettingsRepo _settingsRepo;
        private readonly IRoomRepo _roomRepo;
        private readonly IGroupRepo _groupRepo;
        private readonly ICourseEditionRepo _courseEditionRepo;
        private readonly ISchedulePositionRepo _schedulePositionRepo;

        public SchedulePositionsController(ISettingsRepo settingsRepo, IRoomRepo roomRepo, IGroupRepo groupRepo, ICourseEditionRepo courseEditionRepo, ISchedulePositionRepo schedulePositionRepo)
        {
            _settingsRepo = settingsRepo;
            _roomRepo = roomRepo;
            _groupRepo = groupRepo;
            _courseEditionRepo = courseEditionRepo;
            _schedulePositionRepo = schedulePositionRepo;
        }

        [Authorize(Policy = "Coordinator")]
        [HttpGet]
        [EnableQuery(MaxExpansionDepth = 4)]
        public IActionResult GetScheduleAsCoordinator([FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _schedulePositions = _schedulePositionRepo
                    .Get(e => e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId)
                              && Weeks.Contains(e.CourseRoomTimestamp.Timestamp.Week))
                    .Include(e => e.CourseEdition)
                    .ThenInclude(e => e.Coordinators)
                    .Include(e => e.CourseRoomTimestamp)
                    .ThenInclude(e => e.Timestamp);

                return Ok(_schedulePositions);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            } 
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("Service.GetRoomsAvailability(RoomsIds={RoomsIds},PeriodIndex={PeriodIndex},Day={Day},Weeks={Weeks})")]
        public IActionResult GetRoomsAvailibility([FromODataUri] IEnumerable<int> RoomsIds, [FromODataUri] int PeriodIndex, [FromODataUri] int Day, [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _rooms = _roomRepo
                    .Get(e => RoomsIds.Contains(e.RoomId))
                    .Select(e => e.RoomId);

                var rooms = new Dictionary<int, RoomAvailability>();
                foreach (var _room in _rooms)
                {
                    rooms.TryAdd(_room, new RoomAvailability {RoomId = _room, IsBusy = false});
                }

                var _schedulePositions = _schedulePositionRepo
                    .Get(e => _rooms.Contains(e.RoomId) && e.CourseRoomTimestamp.Timestamp.PeriodIndex == PeriodIndex 
                        && e.CourseRoomTimestamp.Timestamp.Day == Day && Weeks.Contains(e.CourseRoomTimestamp.Timestamp.Week))
                    .Include(e => e.CourseRoomTimestamp)
                        .ThenInclude(e => e.Timestamp)
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

        private static readonly ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>
            CourseEditionLocks = new ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>
            SchedulePositionLocks = new ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>
            CoordinatorPositionLocks = new ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>
            GroupPositionLocks = new ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>();

        private void RemoveLocks(
            ConcurrentQueue<object> courseEditionQueue, 
            CourseEditionKey courseEditionKey, 
            List<ConcurrentQueue<object>> schedulePositionQueues,
            List<SchedulePositionKey> schedulePositionKeys
            )
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
        }

        [Authorize(Policy = "Coordinator")]
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
            ConcurrentQueue<object> courseEditionQueue = null;
            var schedulePositionQueues = new List<ConcurrentQueue<object>>();

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
                        .Include(e => e.Course);

                    if (!_courseEdition.Any())
                    {
                        RemoveLocks(
                            courseEditionQueue,
                            courseEditionKey,
                            schedulePositionQueues,
                            schedulePositionKeys
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
                            schedulePositionKeys
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
                            schedulePositionKeys
                        );

                        return BadRequest("Application settings has not been specified.");
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
                            schedulePositionKeys
                        );

                        return BadRequest("You cannot add any more course units to schedule.");
                    }

                    //get timestamps

                    var coordinatorsIds = courseEdition.Coordinators.Select(e => e.CoordinatorId).ToList();
                    var groupsIds = CourseEditionsController.GetNestedGroupsIds(courseEdition, _groupRepo);

                    //lock ids

                    //look for conflicts and is room available for timestamps

                    //if no conflicts then add schedulepositions

                    //unlock ids

                    RemoveLocks(
                        courseEditionQueue,
                        courseEditionKey,
                        schedulePositionQueues,
                        schedulePositionKeys
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
                        schedulePositionKeys
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
        }
    }
}
