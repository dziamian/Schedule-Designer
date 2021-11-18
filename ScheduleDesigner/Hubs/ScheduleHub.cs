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
        private readonly IUserRepo _userRepo;
        private readonly ICourseEditionRepo _courseEditionRepo;

        private static readonly ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>> CourseEditionLocks = new ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>();

        public ScheduleHub(IUserRepo userRepo, ICourseEditionRepo courseEditionRepo)
        {
            _userRepo = userRepo;
            _courseEditionRepo = courseEditionRepo;
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject LockCourseEdition(int courseId, int courseEditionId)
        {
            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                var courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());

                courseEditionQueue.Enqueue(new object());

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
            catch (DbUpdateConcurrencyException e)
            {
                return new MessageObject {StatusCode = 400, Message = e.Message};
            }
            catch (Exception e)
            {
                return new MessageObject { StatusCode = 400, Message = e.Message };
            }
        }

        [Authorize(Policy = "Coordinator")]
        public MessageObject UnlockCourseEdition(int courseId, int courseEditionId)
        {
            try
            {
                var userId = int.Parse(Context.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var courseEditionKey = new CourseEditionKey { CourseId = courseId, CourseEditionId = courseEditionId };
                var courseEditionQueue = CourseEditionLocks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());

                courseEditionQueue.Enqueue(new object());

                lock (courseEditionQueue)
                {
                    var _user = _userRepo
                        .Get(e => e.UserId == userId && e.LockedCourseEditions.Any(e =>
                            e.CourseId == courseId && e.CourseEditionId == courseEditionId))
                        .Include(e => e.LockedCourseEditions)
                        .Select(e => e.LockedCourseEditions);

                    if (!_user.Any())
                    {
                        courseEditionQueue.TryDequeue(out _);
                        if (courseEditionQueue.IsEmpty)
                        {
                            CourseEditionLocks.TryRemove(courseEditionKey, out _);
                        }

                        return new MessageObject { StatusCode = 404 };
                    }

                    var courseEditionCollection = _user.FirstOrDefaultAsync().Result;
                    var courseEdition = courseEditionCollection.FirstOrDefault();
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
                return new MessageObject {StatusCode = 400, Message = e.Message};
            }
        }

        public override Task OnConnectedAsync()
        {
            var id = int.Parse(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!);

            //Context.User.Claims.ToList().ForEach(Console.WriteLine);
            Console.WriteLine("\tConnected");
            return base.OnConnectedAsync();
        }

        private void RemoveAllClientLocks(int userId, string connectionId)
        {
            var _user = _userRepo
                .Get(e => e.UserId == userId && e.LockedCourseEditions.Any(e => e.LockUserConnectionId == connectionId))
                .Include(e => e.LockedCourseEditions)
                .Select(e => e.LockedCourseEditions);

            var courseEditionCollection = _user.FirstOrDefaultAsync().Result;
            if (courseEditionCollection == null)
            {
                return;
            }

            foreach (var courseEdition in courseEditionCollection)
            {
                UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var id = int.Parse(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!);

            Console.WriteLine("Disconnected");
            RemoveAllClientLocks(id, Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }
}
