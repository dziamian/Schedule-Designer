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
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Hubs
{
    [Authorize]
    public class ScheduleHub : Hub<IScheduleClient>
    {
        private readonly ITimestampRepo _timestampRepo;
        private readonly ICourseEditionRepo _courseEditionRepo;
        private readonly ISchedulePositionRepo _schedulePositionRepo;

        private static readonly ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>
            CourseEditionLocks = new ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>();

        private static readonly ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>
            SchedulePositionLocks = new ConcurrentDictionary<SchedulePositionKey, ConcurrentQueue<object>>();

        public ScheduleHub(ITimestampRepo timestampRepo, ICourseEditionRepo courseEditionRepo, ISchedulePositionRepo schedulePositionRepo)
        {
            _timestampRepo = timestampRepo;
            _courseEditionRepo = courseEditionRepo;
            _schedulePositionRepo = schedulePositionRepo;
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
                        courseEditionQueue.TryDequeue(out _);
                        if (courseEditionQueue.IsEmpty)
                        {
                            CourseEditionLocks.TryRemove(courseEditionKey, out _);
                        }

                        return new MessageObject {StatusCode = 404};
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition.LockUserId != null)
                    {
                        courseEditionQueue.TryDequeue(out _);
                        if (courseEditionQueue.IsEmpty)
                        {
                            CourseEditionLocks.TryRemove(courseEditionKey, out _);
                        }

                        return new MessageObject {StatusCode = 400, Message = "Course edition is already locked."};
                    }

                    courseEdition.LockUserId = userId;
                    courseEdition.LockUserConnectionId = Context.ConnectionId;
                    _courseEditionRepo.Update(courseEdition);

                    var result1 = _courseEditionRepo.SaveChanges().Result;
                    var result2 = Clients.All.LockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);

                    courseEditionQueue.TryDequeue(out _);
                    if (courseEditionQueue.IsEmpty)
                    {
                        CourseEditionLocks.TryRemove(courseEditionKey, out _);
                    }

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
                    courseEditionQueue.TryDequeue(out _);
                    if (courseEditionQueue.IsEmpty)
                    {
                        CourseEditionLocks.TryRemove(courseEditionKey, out _);
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
                        courseEditionQueue.TryDequeue(out _);
                        if (courseEditionQueue.IsEmpty)
                        {
                            CourseEditionLocks.TryRemove(courseEditionKey, out _);
                        }

                        return new MessageObject { StatusCode = 404 };
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition.LockUserId == null)
                    {
                        courseEditionQueue.TryDequeue(out _);
                        if (courseEditionQueue.IsEmpty)
                        {
                            CourseEditionLocks.TryRemove(courseEditionKey, out _);
                        }

                        return new MessageObject { StatusCode = 400, Message = "This course edition is already unlocked." };
                    }

                    if (courseEdition.LockUserId != userId || courseEdition.LockUserConnectionId != Context.ConnectionId)
                    {
                        courseEditionQueue.TryDequeue(out _);
                        if (courseEditionQueue.IsEmpty)
                        {
                            CourseEditionLocks.TryRemove(courseEditionKey, out _);
                        }

                        return new MessageObject { StatusCode = 400, Message = "You cannot unlock this course edition." };
                    }

                    courseEdition.LockUserId = null;
                    courseEdition.LockUserConnectionId = null;
                    _courseEditionRepo.Update(courseEdition);

                    var result1 = _courseEditionRepo.SaveChanges().Result;
                    var result2 = Clients.All.UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);

                    courseEditionQueue.TryDequeue(out _);
                    if (courseEditionQueue.IsEmpty)
                    {
                        CourseEditionLocks.TryRemove(courseEditionKey, out _);
                    }

                    return new MessageObject { StatusCode = 200 };
                }
            }
            catch (Exception e)
            {
                if (!enqueued)
                {
                    return new MessageObject { StatusCode = 400, Message = e.Message };
                }

                courseEditionQueue.TryDequeue(out _);
                if (courseEditionQueue.IsEmpty)
                {
                    CourseEditionLocks.TryRemove(courseEditionKey, out _);
                }

                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject AddSchedulePositions(int courseId, int courseEditionId, int roomId, int periodIndex, int day, int[] weeks)
        {
            CourseEditionKey courseEditionKey = null;
            var schedulePositionKeys = new List<SchedulePositionKey>();
            ConcurrentQueue<object> courseEditionQueue = null;
            var schedulePositionQueues = new List<ConcurrentQueue<object>>();
            
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
                    //do some stuff

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

                    return new MessageObject { StatusCode = 200 };
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
                    return new MessageObject { StatusCode = 400, Message = e.Message };
                }

                Monitor.Enter(courseEditionQueue);
                foreach (var schedulePositionQueue in schedulePositionQueues)
                {
                    Monitor.Enter(schedulePositionQueue);
                }

                try
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
                finally
                {
                    Monitor.Exit(courseEditionQueue);
                    foreach (var schedulePositionQueue in schedulePositionQueues)
                    {
                        Monitor.Exit(schedulePositionQueue);
                    }
                }

                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject ModifySchedulePositions()
        {
            return new MessageObject { StatusCode = 400 };
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject RemoveSchedulePositions()
        {
            return new MessageObject { StatusCode = 400 };
        }

        private void RemoveAllClientLocks(int userId, string connectionId)
        {
            /*var _user = _userRepo
                .Get(e => e.UserId == userId && e.LockedCourseEditions.Any(e => e.LockUserConnectionId == connectionId))
                .Include(e => e.LockedCourseEditions)
                .Select(e => e.LockedCourseEditions);*/
            var _courseEditions = _courseEditionRepo
                .Get(e => e.LockUserId == userId && e.LockUserConnectionId == connectionId);

            if (!_courseEditions.Any())
            {
                return;
            }

            var courseEditions = _courseEditions.ToList();

            foreach (var courseEdition in courseEditions)
            {
                UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);
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
