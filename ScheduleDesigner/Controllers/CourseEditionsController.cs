using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Hubs.Interfaces;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("CourseEditions")]
    public class CourseEditionsController : ODataController
    {
        private readonly ICourseEditionRepo _courseEditionRepo;
        private readonly ISettingsRepo _settingsRepo;
        private readonly IHubContext<ScheduleHub, IScheduleClient> _hubContext;

        private static readonly ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>> Locks = new ConcurrentDictionary<CourseEditionKey, ConcurrentQueue<object>>();

        public CourseEditionsController(ICourseEditionRepo courseEditionRepo, ISettingsRepo settingsRepo, IHubContext<ScheduleHub, IScheduleClient> hubContext)
        {
            _courseEditionRepo = courseEditionRepo;
            _settingsRepo = settingsRepo;
            _hubContext = hubContext;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateCourseEdition([FromBody] CourseEdition courseEdition)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseEdition = await _courseEditionRepo.Add(courseEdition);

                if (_courseEdition != null)
                {
                    await _courseEditionRepo.SaveChanges();
                    return Created(_courseEdition);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize(Policy = "Coordinator")]
        [HttpPost]
        [ODataRoute("({key1},{key2})/Service.Lock")]
        public IActionResult Lock([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);

                var courseEditionKey = new CourseEditionKey {CourseId = key1, CourseEditionId = key2};
                var courseEditionQueue = Locks.GetOrAdd(courseEditionKey, new ConcurrentQueue<object>());

                courseEditionQueue.Enqueue(new object());

                lock (courseEditionQueue)
                {
                    var _courseEdition = _courseEditionRepo
                        .Get(e => e.Coordinators.Any(e => e.CoordinatorId == userId) && e.CourseId == key1 &&
                                  e.CourseEditionId == key2)
                        .Include(e => e.Coordinators);

                    if (!_courseEdition.Any())
                    {
                        courseEditionQueue.TryDequeue(out _);
                        if (courseEditionQueue.IsEmpty)
                        {
                            Locks.TryRemove(courseEditionKey, out _);
                        }

                        return NotFound();
                    }

                    var courseEdition = _courseEdition.FirstOrDefault();
                    if (courseEdition.LockUserId != null)
                    {
                        courseEditionQueue.TryDequeue(out _);
                        if (courseEditionQueue.IsEmpty)
                        {
                            Locks.TryRemove(courseEditionKey, out _);
                        }

                        return BadRequest("Course edition is already locked.");
                    }

                    courseEdition.LockUserId = userId;
                    _courseEditionRepo.Update(courseEdition);

                    var result = _courseEditionRepo.SaveChanges().Result;
                    var result2 = _hubContext.Clients.All.LockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);

                    courseEditionQueue.TryDequeue(out _);
                    if (courseEditionQueue.IsEmpty)
                    {
                        Locks.TryRemove(courseEditionKey, out _);
                    }

                    return Ok();
                }
            }
            catch (DbUpdateConcurrencyException e)
            {
                return BadRequest(e.Message);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize(Policy = "Coordinator")]
        [HttpPost]
        [ODataRoute("({key1},{key2})/Service.Unlock")]
        public async Task<IActionResult> Unlock([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);

                var _courseEdition = _courseEditionRepo
                    .Get(e => e.Coordinators.Any(e => e.CoordinatorId == userId) && e.CourseId == key1 && e.CourseEditionId == key2)
                    .Include(e => e.Coordinators);

                if (!_courseEdition.Any())
                {
                    return NotFound();
                }

                var courseEdition = await _courseEdition.FirstOrDefaultAsync();
                if (courseEdition.LockUserId == null)
                {
                    return BadRequest("This course edition is already unlocked.");
                }

                if (courseEdition.LockUserId != userId)
                {
                    return BadRequest("You cannot unlock this course edition.");
                }

                courseEdition.LockUserId = null;
                _courseEditionRepo.Update(courseEdition);

                await _courseEditionRepo.SaveChanges();
                await _hubContext.Clients.All.UnlockCourseEdition(courseEdition.CourseId, courseEdition.CourseEditionId);

                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [EnableQuery(MaxExpansionDepth = 3)]
        [HttpGet]
        public async Task<IActionResult> GetMyCourseEditions([FromODataUri] double Frequency)
        {
            var _settings = await _settingsRepo.GetSettings();
            if (_settings == null)
            {
                return BadRequest("Application settings has not been specified.");
            }

            if (Frequency > _settings.TermDurationWeeks || Frequency <= 0)
            {
                return BadRequest("Frequency is invalid");
            }

            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);

                var courseDurationMinutes = _settings.CourseDurationMinutes;
                var totalMinutes = Frequency * courseDurationMinutes;

                var _courseEditions = _courseEditionRepo
                    .Get(e => e.Coordinators.Any(e => e.CoordinatorId == userId) && e.Course.UnitsMinutes - e.SchedulePositions.Count * courseDurationMinutes >= totalMinutes)
                    .Include(e => e.SchedulePositions)
                    .Include(e => e.Course)
                    .Include(e => e.Coordinators);

                return Ok(_courseEditions);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("")]
        public IActionResult GetCourseEditions()
        {
            return Ok(_courseEditionRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2})")]
        public IActionResult GetCourseEdition([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _courseEdition = _courseEditionRepo
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2);
                if (!_courseEdition.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_courseEdition));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> UpdateCourseEdition([FromODataUri] int key1, [FromODataUri] int key2, [FromBody] Delta<CourseEdition> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseEdition = await _courseEditionRepo
                    .GetFirst(e => e.CourseId == key1 && e.CourseEditionId == key2);
                if (_courseEdition == null)
                {
                    return NotFound();
                }

                delta.Patch(_courseEdition);

                await _courseEditionRepo.SaveChanges();

                return Ok(_courseEdition);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> DeleteCourseEdition([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var result = await _courseEditionRepo
                    .Delete(e => e.CourseId == key1 && e.CourseEditionId == key2);
                if (result < 0)
                {
                    return NotFound();
                }

                await _courseEditionRepo.SaveChanges();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
