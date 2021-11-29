using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ScheduleDesigner.Hubs.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Controllers;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Hubs
{
    [Authorize]
    public class ScheduleHub : Hub<IScheduleClient>
    {
        private readonly ISettingsRepo _settingsRepo;
        private readonly ITimestampRepo _timestampRepo;
        private readonly ICourseEditionRepo _courseEditionRepo;
        private readonly ICourseRoomTimestampRepo _courseRoomTimestampRepo;
        private readonly ISchedulePositionRepo _schedulePositionRepo;
        private readonly IGroupRepo _groupRepo;

        private static readonly ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>
            CourseEditionLocks = new ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>
            SchedulePositionLocks = new ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>
            CoordinatorPositionLocks = new ConcurrentDictionary<CoordinatorPositionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>
            GroupPositionLocks = new ConcurrentDictionary<GroupPositionKey, ConcurrentQueue<object>>();

        private void RemoveCourseEditionLock(ConcurrentQueue<object> courseEditionQueue, CourseEditionKey courseEditionKey)
        {
            courseEditionQueue.TryDequeue(out _);
            if (courseEditionQueue.IsEmpty)
            {
                CourseEditionLocks.TryRemove(courseEditionKey, out _);
            }
        }

        private void RemoveSchedulePositionsLocks(List<ConcurrentQueue<object>> schedulePositionQueues, List<SchedulePositionKey> schedulePositionKeys)
        {
            for (var i = 0; i < schedulePositionQueues.Count; ++i)
            {
                schedulePositionQueues[i].TryDequeue(out _);
                if (schedulePositionQueues[i].IsEmpty)
                {
                    SchedulePositionLocks.TryRemove(schedulePositionKeys[i], out _);
                }
            }
        }

        private void RemoveCoordinatorPositionsLocks(List<ConcurrentQueue<object>> coordinatorPositionQueues, List<CoordinatorPositionKey> coordinatorPositionKeys)
        {
            for (var i = 0; i < coordinatorPositionQueues.Count; ++i)
            {
                coordinatorPositionQueues[i].TryDequeue(out _);
                if (coordinatorPositionQueues[i].IsEmpty)
                {
                    CoordinatorPositionLocks.TryRemove(coordinatorPositionKeys[i], out _);
                }
            }
        }

        private void RemoveGroupPositionsLocks(List<ConcurrentQueue<object>> groupPositionQueues, List<GroupPositionKey> groupPositionKeys)
        {
            for (var i = 0; i < groupPositionQueues.Count; ++i)
            {
                groupPositionQueues[i].TryDequeue(out _);
                if (groupPositionQueues[i].IsEmpty)
                {
                    GroupPositionLocks.TryRemove(groupPositionKeys[i], out _);
                }
            }
        }

        private void RemoveLocks(
            ConcurrentQueue<object> courseEditionQueue,
            CourseEditionKey courseEditionKey,
            List<ConcurrentQueue<object>> schedulePositionQueues,
            List<SchedulePositionKey> schedulePositionKeys,
            List<ConcurrentQueue<object>> coordinatorPositionQueues,
            List<CoordinatorPositionKey> coordinatorPositionKeys,
            List<ConcurrentQueue<object>> groupPositionQueues,
            List<GroupPositionKey> groupPositionKeys)
        {
            RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
            RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);
            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
            RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);
        }


        public ScheduleHub(
            ISettingsRepo settingsRepo,
            ITimestampRepo timestampRepo, 
            ICourseEditionRepo courseEditionRepo, 
            ICourseRoomTimestampRepo courseRoomTimestampRepo,
            ISchedulePositionRepo schedulePositionRepo,
            IGroupRepo groupRepo
            )
        {
            _settingsRepo = settingsRepo;
            _timestampRepo = timestampRepo;
            _courseEditionRepo = courseEditionRepo;
            _courseRoomTimestampRepo = courseRoomTimestampRepo;
            _schedulePositionRepo = schedulePositionRepo;
            _groupRepo = groupRepo;
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject LockCourseEdition(int courseId, int courseEditionId)
        {
            CourseEditionKey courseEditionKey = null;
            ConcurrentQueue<object> courseEditionQueue = null;
            
            var enqueued = false;
            
            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());

                courseEditionQueue.Enqueue(new object());

                enqueued = true;

                lock (courseEditionQueue)
                {
                    var _courseEdition = _courseEditionRepo
                        .Get(e => e.Coordinators.Any(e => e.CoordinatorId == userId) && e.CourseId == courseId &&
                                  e.CourseEditionId == courseEditionId)
                        .Include(e => e.Coordinators);

                    if (!_courseEdition.Any())
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                        
                        return new MessageObject {StatusCode = 404};
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (!(courseEdition is {LockUserId: null}))
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                        
                        return new MessageObject {StatusCode = 400, Message = "Someone has locked this course before you." };
                    }

                    courseEdition.LockUserId = userId;
                    courseEdition.LockUserConnectionId = Context.ConnectionId;
                    _courseEditionRepo.Update(courseEdition);

                    var result1 = _courseEditionRepo.SaveChanges().Result;
                    var result2 = Clients.All.LockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);

                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                    return new MessageObject { StatusCode = 200 };
                }
            }
            catch (Exception e)
            {
                if (!enqueued)
                {
                    return new MessageObject {StatusCode = 400, Message = e.Message};
                }

                lock (courseEditionQueue)
                {
                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                }

                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject LockSchedulePositions(int roomId, int periodIndex, int day, int[] weeks)
        {
            var schedulePositionKeys = new List<SchedulePositionKey>();
            var schedulePositionQueues = new List<ConcurrentQueue<object>>();

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

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

                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    var _timestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                    if (!_timestamps.Any())
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject {StatusCode = 404, Message = "Could not find requested time periods."};
                    }

                    var _schedulePositions = _schedulePositionRepo
                        .Get(e => _timestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Coordinators);

                    if (_schedulePositions.Count() != weeks.Length)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule."};
                    }

                    if (Enumerable.Any(_schedulePositions, schedulePosition => schedulePosition.LockUserId != null))
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 400, Message = "Some positions in schedule are already locked." };
                    }

                    var courseEdition = _schedulePositions.FirstOrDefault()?.CourseEdition;
                    if (courseEdition == null)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 400, Message = "There was an unexpected error." };
                    }

                    foreach (var schedulePosition in _schedulePositions)
                    {
                        schedulePosition.LockUserId = userId;
                        schedulePosition.LockUserConnectionId = Context.ConnectionId;
                    }

                    _schedulePositionRepo.GetAll().UpdateRange(_schedulePositions);

                    var result1 = _schedulePositionRepo.SaveChanges().Result;
                    var result2 = Clients.All.LockSchedulePositions(
                        courseEdition.CourseId, courseEdition.CourseEditionId,
                        roomId, periodIndex, 
                        day, weeks
                    );

                    RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                    return new MessageObject { StatusCode = 200 };
                }
                finally
                {
                    foreach (var schedulePositionQueue in schedulePositionQueues)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);
                }
                finally
                {
                    foreach (var schedulePositionQueue in schedulePositionQueues)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }

                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject UnlockCourseEdition(int courseId, int courseEditionId)
        {
            CourseEditionKey courseEditionKey = null;
            ConcurrentQueue<object> courseEditionQueue = null;
            
            var enqueued = false;
            
            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());

                courseEditionQueue.Enqueue(new object());

                enqueued = true;

                lock (courseEditionQueue)
                {
                    var _courseEdition = _courseEditionRepo
                        .Get(e => e.Coordinators.Any(e => e.CoordinatorId == userId) && e.CourseId == courseId &&
                                  e.CourseEditionId == courseEditionId)
                        .Include(e => e.Coordinators);

                    if (!_courseEdition.Any())
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                        return new MessageObject { StatusCode = 404 };
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition.LockUserId == null)
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                        return new MessageObject { StatusCode = 400, Message = "This course edition is already unlocked." };
                    }

                    if (courseEdition.LockUserId != userId || courseEdition.LockUserConnectionId != Context.ConnectionId)
                    {
                        RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                        return new MessageObject { StatusCode = 400, Message = "You cannot unlock this course edition." };
                    }

                    courseEdition.LockUserId = null;
                    courseEdition.LockUserConnectionId = null;
                    _courseEditionRepo.Update(courseEdition);

                    var result1 = _courseEditionRepo.SaveChanges().Result;
                    var result2 = Clients.All.UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);

                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);

                    return new MessageObject { StatusCode = 200 };
                }
            }
            catch (Exception e)
            {
                if (!enqueued)
                {
                    return new MessageObject { StatusCode = 400, Message = e.Message };
                }

                lock (courseEditionQueue)
                {
                    RemoveCourseEditionLock(courseEditionQueue, courseEditionKey);
                }

                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject UnlockSchedulePositions(int roomId, int periodIndex, int day, int[] weeks)
        {
            var schedulePositionKeys = new List<SchedulePositionKey>();
            var schedulePositionQueues = new List<ConcurrentQueue<object>>();

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

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

                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    var _timestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                    if (!_timestamps.Any())
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 404, Message = "Could not find requested time periods." };
                    }

                    var _schedulePositions = _schedulePositionRepo
                        .Get(e => _timestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Coordinators);

                    if (_schedulePositions.Count() != weeks.Length)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." };
                    }

                    foreach (var schedulePosition in _schedulePositions)
                    {
                        if (schedulePosition.LockUserId == null)
                        {
                            RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                            return new MessageObject { StatusCode = 400, Message = "Some positions in schedule are already unlocked." };
                        }

                        if (schedulePosition.LockUserId != userId || schedulePosition.LockUserConnectionId != Context.ConnectionId)
                        {
                            RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                            return new MessageObject { StatusCode = 400, Message = "You cannot unlock some of positions in the schedule." };
                        }
                    }

                    var courseEdition = _schedulePositions.FirstOrDefault()?.CourseEdition;
                    if (courseEdition == null)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 400, Message = "There was an unexpected error." };
                    }

                    foreach (var schedulePosition in _schedulePositions)
                    {
                        schedulePosition.LockUserId = null;
                        schedulePosition.LockUserConnectionId = null;
                    }

                    _schedulePositionRepo.GetAll().UpdateRange(_schedulePositions);

                    var result1 = _schedulePositionRepo.SaveChanges().Result;
                    var result2 = Clients.All.UnlockSchedulePositions(
                        courseEdition.CourseId, courseEdition.CourseEditionId,
                        roomId, periodIndex, 
                        day, weeks);

                    RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                    return new MessageObject { StatusCode = 200 };
                }
                finally
                {
                    foreach (var schedulePositionQueue in schedulePositionQueues)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);
                }
                finally
                {
                    foreach (var schedulePositionQueue in schedulePositionQueues)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }

                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        
        [Authorize(Policy = "Coordinator")]
        public MessageObject AddSchedulePositions(int courseId, int courseEditionId, int roomId, int periodIndex, int day, int[] weeks)
        {
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
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

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

                        return new MessageObject {StatusCode = 404};
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition.LockUserId != userId || courseEdition.LockUserConnectionId != Context.ConnectionId)
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

                        return new MessageObject{StatusCode = 400, Message = "You didn't lock this course edition."};
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

                        return new MessageObject {StatusCode = 400, Message = "Application settings has not been specified."};
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

                        return new MessageObject { StatusCode = 400, Message = "Chosen room does not exist or has not been assigned to this course."};
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

                        return new MessageObject { StatusCode = 400, Message = "You cannot add any more units of this course to the schedule."};
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
                                    { CoordinatorId = coordinatorId, PeriodIndex = periodIndex, Day = day, Week = week };
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

                        if (_timestamps.Count != weeks.Length)
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

                            return new MessageObject { StatusCode = 404, Message = "Could not find requested time periods." };
                        }

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
                            .Select(e => new { e.TimestampId, e.RoomId });

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

                            return new MessageObject {StatusCode = 400, Message = "Some conflicts with other courses occurred."};
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
                        var result2 = Clients.All.AddedSchedulePositions(
                            courseEdition.CourseId, courseEdition.CourseEditionId,
                            groupsIds, coordinatorsIds,
                            roomId, periodIndex,
                            day, weeks
                        );

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

                        return new MessageObject { StatusCode = 200 };
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
                    return new MessageObject {StatusCode = 400, Message = e.Message};
                }

                Monitor.Enter(courseEditionQueue);
                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
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
                    foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                    {
                        Monitor.Exit(coordinatorPositionQueue);
                    }
                    foreach (var groupPositionQueue in groupPositionQueues)
                    {
                        Monitor.Exit(groupPositionQueue);
                    }
                }

                return new MessageObject {StatusCode = 400, Message = e.Message};
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject ModifySchedulePositions(int roomId, int periodIndex, int day, int[] weeks, int destRoomId, int destPeriodIndex, int destDay, int[] destWeeks)
        {
            var schedulePositionKeys1 = new List<SchedulePositionKey>();
            var schedulePositionKeys2 = new List<SchedulePositionKey>();
            var coordinatorPositionKeys = new List<CoordinatorPositionKey>();
            var groupPositionKeys = new List<GroupPositionKey>();
            var schedulePositionQueues1 = new List<ConcurrentQueue<object>>();
            var schedulePositionQueues2 = new List<ConcurrentQueue<object>>();
            var coordinatorPositionQueues = new List<ConcurrentQueue<object>>();
            var groupPositionQueues = new List<ConcurrentQueue<object>>();

            Array.Sort(weeks);
            Array.Sort(destWeeks);

            if (weeks.Length != destWeeks.Length)
            {
                return new MessageObject {StatusCode = 400, Message = "Amount of weeks must be equal."};
            }

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                lock (SchedulePositionLocks)
                {
                    foreach (var week in weeks)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, PeriodIndex = periodIndex, Day = day, Week = week };
                        schedulePositionKeys1.Add(key);
                        var queue = SchedulePositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                        schedulePositionQueues1.Add(queue);
                        queue.Enqueue(new object());
                    }

                    foreach (var week in destWeeks)
                    {
                        var key = new SchedulePositionKey { RoomId = roomId, PeriodIndex = periodIndex, Day = day, Week = week };
                        schedulePositionKeys2.Add(key);
                        var queue = SchedulePositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                        schedulePositionQueues2.Add(queue);
                        queue.Enqueue(new object());
                    }
                }

                foreach (var schedulePositionQueue1 in schedulePositionQueues1)
                {
                    Monitor.Enter(schedulePositionQueue1);
                }
                foreach (var schedulePositionQueue2 in schedulePositionQueues2)
                {
                    Monitor.Enter(schedulePositionQueue2);
                }

                try
                {
                    var _sourceTimestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                    if (_sourceTimestamps.Count != weeks.Length)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues1, schedulePositionKeys1);
                        RemoveSchedulePositionsLocks(schedulePositionQueues2, schedulePositionKeys2);

                        return new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." };
                    }

                    var _sourceSchedulePositions = _schedulePositionRepo
                        .Get(e => _sourceTimestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Coordinators);

                    if (_sourceSchedulePositions.Count() != _sourceTimestamps.Count)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues1, schedulePositionKeys1);
                        RemoveSchedulePositionsLocks(schedulePositionQueues2, schedulePositionKeys2);

                        return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." };
                    }

                    if (Enumerable.Any(_sourceSchedulePositions, schedulePosition => schedulePosition.LockUserId != userId || schedulePosition.LockUserConnectionId != Context.ConnectionId))
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues1, schedulePositionKeys1);
                        RemoveSchedulePositionsLocks(schedulePositionQueues2, schedulePositionKeys2);

                        return new MessageObject { StatusCode = 400, Message = "You didn't lock some positions in schedule." };
                    }

                    var courseEdition = _sourceSchedulePositions.FirstOrDefault()?.CourseEdition;

                    if (courseEdition == null)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues1, schedulePositionKeys1);
                        RemoveSchedulePositionsLocks(schedulePositionQueues2, schedulePositionKeys2);

                        return new MessageObject { StatusCode = 400, Message = "Could not find course edition for requested positions in schedule." };
                    }
                    
                    var _courseEdition = _courseEditionRepo
                        .Get(e => e.CourseId == courseEdition.CourseId &&
                                  e.CourseEditionId == courseEdition.CourseEditionId)
                        .Include(e => e.Course)
                            .ThenInclude(e => e.Rooms)
                        .Include(e => e.Coordinators)
                        .Include(e => e.Groups)
                            .ThenInclude(e => e.Group);

                    var includableCourseEdition = _courseEdition.FirstOrDefault();
                    if (includableCourseEdition == null || !includableCourseEdition.Course.Rooms.Select(e => e.RoomId).Contains(destRoomId))
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues1, schedulePositionKeys1);
                        RemoveSchedulePositionsLocks(schedulePositionQueues2, schedulePositionKeys2);

                        return new MessageObject { StatusCode = 400, Message = "Chosen room does not exist or has not been assigned to chosen course." };
                    }

                    var coordinatorsIds = includableCourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                    var groupsIds = CourseEditionsController.GetNestedGroupsIds(includableCourseEdition, _groupRepo).ToArray();
                    Array.Sort(coordinatorsIds);
                    Array.Sort(groupsIds);

                    lock (CoordinatorPositionLocks)
                    lock (GroupPositionLocks)
                    {
                        foreach (var week in destWeeks)
                        {
                            foreach (var coordinatorId in coordinatorsIds)
                            {
                                var key = new CoordinatorPositionKey
                                    { CoordinatorId = coordinatorId, PeriodIndex = destPeriodIndex, Day = destDay, Week = week };
                                coordinatorPositionKeys.Add(key);
                                var queue = CoordinatorPositionLocks.GetOrAdd(key, new ConcurrentQueue<object>());
                                coordinatorPositionQueues.Add(queue);
                                queue.Enqueue(new object());
                            }

                            foreach (var groupId in groupsIds)
                            {
                                var key = new GroupPositionKey
                                    { GroupId = groupId, PeriodIndex = destPeriodIndex, Day = destDay, Week = week };
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
                        var _destTimestamps = _timestampRepo
                            .Get(e => e.PeriodIndex == destPeriodIndex && e.Day == destDay && destWeeks.Contains(e.Week))
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e).ToList();

                        if (_destTimestamps.Count != destWeeks.Length)
                        {
                            RemoveSchedulePositionsLocks(schedulePositionQueues1, schedulePositionKeys1);
                            RemoveSchedulePositionsLocks(schedulePositionQueues2, schedulePositionKeys2);
                            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
                            RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);

                            return new MessageObject { StatusCode = 404, Message = "Could not find requested destination time periods." };
                        }

                        var _destSchedulePositions = _sourceTimestamps.SequenceEqual(_destTimestamps) 
                            ? 
                            _schedulePositionRepo
                                    .Get(e => _destTimestamps.Contains(e.TimestampId) && e.RoomId == destRoomId)
                                    .Include(e => e.CourseEdition)
                                    .ThenInclude(e => e.Coordinators)
                                    .Include(e => e.CourseEdition)
                                    .ThenInclude(e => e.Groups)
                                    .Select(e => new { e.TimestampId, e.RoomId })
                            : 
                            _schedulePositionRepo
                            .Get(e => _destTimestamps.Contains(e.TimestampId)
                                      && (e.RoomId == destRoomId || e.CourseEdition.Coordinators
                                                                 .Select(e => e.CoordinatorId)
                                                                 .Any(e => coordinatorsIds.Contains(e))
                                                             || e.CourseEdition.Groups.Select(e => e.GroupId)
                                                                 .Any(e => groupsIds.Contains(e))))
                            .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Coordinators)
                            .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Groups)
                            .Select(e => new { e.TimestampId, e.RoomId });

                        if (_destSchedulePositions.Any())
                        {
                            RemoveSchedulePositionsLocks(schedulePositionQueues1, schedulePositionKeys1);
                            RemoveSchedulePositionsLocks(schedulePositionQueues2, schedulePositionKeys2);
                            RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
                            RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);

                            return new MessageObject { StatusCode = 400, Message = "Some conflicts with other courses occurred." };
                        }

                        var _courseRoomTimestamps = _courseRoomTimestampRepo
                            .Get(e => e.RoomId == destRoomId && _destTimestamps.Contains(e.TimestampId) &&
                                      e.CourseId == courseEdition.CourseId)
                            .Select(e => e.TimestampId)
                            .OrderBy(e => e);

                        var destSchedulePositions = _destTimestamps.Select(timestampId => new SchedulePosition
                        {
                            RoomId = destRoomId,
                            TimestampId = timestampId,
                            CourseId = courseEdition.CourseId,
                            CourseEditionId = courseEdition.CourseEditionId,
                            CourseRoomTimestamp = !_courseRoomTimestamps.Contains(timestampId) ? new CourseRoomTimestamp
                            {
                                RoomId = destRoomId,
                                TimestampId = timestampId,
                                CourseId = courseEdition.CourseId
                            } : null
                        }).ToList();

                        _schedulePositionRepo.GetAll().RemoveRange(_sourceSchedulePositions);
                        _schedulePositionRepo.GetAll().AddRange(destSchedulePositions);

                        var result1 = _schedulePositionRepo.SaveChanges().Result;
                        var result2 = Clients.All.ModifiedSchedulePositions(
                            includableCourseEdition.CourseId, includableCourseEdition.CourseEditionId,
                            groupsIds, coordinatorsIds,
                            roomId, destRoomId,
                            periodIndex, destPeriodIndex,
                            day, destDay,
                            weeks, destWeeks
                        );

                        RemoveSchedulePositionsLocks(schedulePositionQueues1, schedulePositionKeys1);
                        RemoveSchedulePositionsLocks(schedulePositionQueues2, schedulePositionKeys2);
                        RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
                        RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);

                        return new MessageObject { StatusCode = 200 };
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
                }
                finally
                {
                    foreach (var schedulePositionQueue1 in schedulePositionQueues1)
                    {
                        Monitor.Exit(schedulePositionQueue1);
                    }
                    foreach (var schedulePositionQueue2 in schedulePositionQueues2)
                    {
                        Monitor.Exit(schedulePositionQueue2);
                    }
                }
            }
            catch (Exception e)
            {
                foreach (var schedulePositionQueue1 in schedulePositionQueues1)
                {
                    Monitor.Enter(schedulePositionQueue1);
                }
                foreach (var schedulePositionQueue2 in schedulePositionQueues2)
                {
                    Monitor.Enter(schedulePositionQueue2);
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
                    RemoveSchedulePositionsLocks(schedulePositionQueues1, schedulePositionKeys1);
                    RemoveSchedulePositionsLocks(schedulePositionQueues2, schedulePositionKeys2);
                    RemoveCoordinatorPositionsLocks(coordinatorPositionQueues, coordinatorPositionKeys);
                    RemoveGroupPositionsLocks(groupPositionQueues, groupPositionKeys);
                }
                finally
                {
                    foreach (var schedulePositionQueue1 in schedulePositionQueues1)
                    {
                        Monitor.Exit(schedulePositionQueue1);
                    }
                    foreach (var schedulePositionQueue2 in schedulePositionQueues2)
                    {
                        Monitor.Exit(schedulePositionQueue2);
                    }
                    foreach (var coordinatorPositionQueue in coordinatorPositionQueues)
                    {
                        Monitor.Exit(coordinatorPositionQueue);
                    }
                    foreach (var groupPositionQueue in groupPositionQueues)
                    {
                        Monitor.Exit(groupPositionQueue);
                    }
                }

                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject RemoveSchedulePositions(int roomId, int periodIndex, int day, int[] weeks)
        {
            var schedulePositionKeys = new List<SchedulePositionKey>();
            var schedulePositionQueues = new List<ConcurrentQueue<object>>();

            Array.Sort(weeks);

            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

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

                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    var _timestamps = _timestampRepo
                        .Get(e => e.PeriodIndex == periodIndex && e.Day == day && weeks.Contains(e.Week))
                        .Select(e => e.TimestampId)
                        .OrderBy(e => e).ToList();

                    if (_timestamps.Count != weeks.Length)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 404, Message = "Could not find requested source time periods." };
                    }

                    var _schedulePositions = _schedulePositionRepo
                        .Get(e => _timestamps.Contains(e.TimestampId) && e.RoomId == roomId &&
                                  e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId))
                        .Include(e => e.CourseEdition)
                            .ThenInclude(e => e.Coordinators);

                    if (_schedulePositions.Count() != _timestamps.Count)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 404, Message = "Could not find requested positions in schedule." };
                    }

                    if (Enumerable.Any(_schedulePositions, schedulePosition => schedulePosition.LockUserId != userId || schedulePosition.LockUserConnectionId != Context.ConnectionId))
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 400, Message = "You didn't lock some positions in schedule." };
                    }

                    var firstCourseEdition = _schedulePositions.FirstOrDefault()?.CourseEdition;
                    if (firstCourseEdition == null)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 400, Message = "There was an unexpected error." };
                    }

                    var _courseEdition = _courseEditionRepo
                        .Get(e => e.CourseId == firstCourseEdition.CourseId &&
                                  e.CourseEditionId == firstCourseEdition.CourseEditionId)
                        .Include(e => e.Groups)
                        .ThenInclude(e => e.Group);

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition == null)
                    {
                        RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                        return new MessageObject { StatusCode = 400, Message = "There was an unexpected error." };
                    }

                    var coordinatorsIds = firstCourseEdition.Coordinators.Select(e => e.CoordinatorId).ToArray();
                    var groupsIds = CourseEditionsController.GetNestedGroupsIds(courseEdition, _groupRepo).ToArray();
                    Array.Sort(coordinatorsIds);
                    Array.Sort(groupsIds);

                    _schedulePositionRepo.GetAll().RemoveRange(_schedulePositions);
                    var result1 = _schedulePositionRepo.SaveChanges().Result;
                    var result2 = Clients.All.RemovedSchedulePositions(
                        courseEdition.CourseId, courseEdition.CourseEditionId,
                        groupsIds, coordinatorsIds,
                        roomId, periodIndex,
                        day, weeks
                    );

                    RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);

                    return new MessageObject {StatusCode = 204};
                }
                finally
                {
                    foreach (var schedulePositionQueue in schedulePositionQueues)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }
            }
            catch (Exception e)
            {
                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
                {
                    RemoveSchedulePositionsLocks(schedulePositionQueues, schedulePositionKeys);
                }
                finally
                {
                    foreach (var schedulePositionQueue in schedulePositionQueues)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }

                return new MessageObject {StatusCode = 400, Message = e.Message};
            }
        }

        private void RemoveAllClientLocks(int userId, string connectionId)
        {
            var _courseEditions = _courseEditionRepo
                .Get(e => e.LockUserId == userId && e.LockUserConnectionId == connectionId);

            var _schedulePositions = _schedulePositionRepo
                .Get(e => e.LockUserId == userId && e.LockUserConnectionId == connectionId)
                .Include(e => e.CourseRoomTimestamp)
                    .ThenInclude(e => e.Timestamp);

            var courseEditions = _courseEditions.Any() ? _courseEditions.ToList() : new List<CourseEdition>();
            var schedulePositions = _schedulePositions.Any() ? _schedulePositions.ToList() : new List<SchedulePosition>();

            foreach (var courseEdition in courseEditions)
            {
                UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);
            }

            foreach (var schedulePosition in schedulePositions)
            {
                var timestamp = schedulePosition.CourseRoomTimestamp.Timestamp;
                UnlockSchedulePositions(schedulePosition.RoomId, timestamp.PeriodIndex, timestamp.Day, new int[]{timestamp.Week});
            }
        }

        public override Task OnConnectedAsync()
        {
            var id = int.Parse(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!);

            //Context.User.Claims.ToList().ForEach(Console.WriteLine);
            Console.WriteLine($"\tConnected {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var id = int.Parse(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!);

            Console.WriteLine($"Disconnected {Context.ConnectionId}");
            RemoveAllClientLocks(id, Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
