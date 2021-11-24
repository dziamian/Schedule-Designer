using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ScheduleDesigner.Hubs.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Linq;
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
        private readonly ICourseEditionRepo _courseEditionRepo;

        private static readonly ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>> CourseEditionLocks = new ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>();

        public ScheduleHub(ICourseEditionRepo courseEditionRepo)
        {
            _courseEditionRepo = courseEditionRepo;
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

                    return new MessageObject {StatusCode = 200};
                }
            }
            catch (Exception e)
            {
                if (!enqueued)
                {
                    return new MessageObject {StatusCode = 400, Message = e.Message};
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

                        return new MessageObject { StatusCode = 400, Message = $"You cannot unlock this course edition. {courseEdition.CourseId},{courseEdition.CourseEditionId}" };
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

                return new MessageObject {StatusCode = 400, Message = e.Message};
            }
        }

        public override Task OnConnectedAsync()
        {
            var id = int.Parse(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!);

            //Context.User.Claims.ToList().ForEach(Console.WriteLine);
            Console.WriteLine($"\tConnected {Context.ConnectionId}");
            return base.OnConnectedAsync();
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

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var id = int.Parse(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!);

            Console.WriteLine($"Disconnected {Context.ConnectionId}");
            RemoveAllClientLocks(id, Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
