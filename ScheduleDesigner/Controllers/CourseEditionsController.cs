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
        private readonly IGroupRepo _groupRepo;
        private readonly ICourseEditionRepo _courseEditionRepo;
        private readonly ISchedulePositionRepo _schedulePositionRepo;
        private readonly ISettingsRepo _settingsRepo;

        public CourseEditionsController(IGroupRepo groupRepo, ICourseEditionRepo courseEditionRepo, ISchedulePositionRepo schedulePositionRepo, ISettingsRepo settingsRepo)
        {
            _groupRepo = groupRepo;
            _courseEditionRepo = courseEditionRepo;
            _schedulePositionRepo = schedulePositionRepo;
            _settingsRepo = settingsRepo;
        }

        public static List<int> GetNestedGroupsIds(CourseEdition courseEdition, IGroupRepo _groupRepo)
        {
            var groups = courseEdition.Groups.Select(e => e.Group).ToList();
            var groupsIds = groups.Select(e => e.GroupId).ToList();

            var startIndex = 0;
            var endIndex = groups.Count;
            var oldSize = endIndex;
            while (groups.GetRange(startIndex, endIndex - startIndex).Any(e => e.ParentGroupId != null))
            {
                var _parentGroups = _groupRepo
                    .Get(e => groupsIds.GetRange(startIndex, endIndex - startIndex).Contains(e.GroupId) && e.ParentGroup != null)
                    .Include(e => e.ParentGroup)
                    .Select(e => e.ParentGroup);

                groups.AddRange(_parentGroups);
                groupsIds.AddRange(_parentGroups.Select(e => e.GroupId).ToList());

                startIndex = endIndex;
                endIndex = groups.Count;
            }

            var _childGroups = _groupRepo
                .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(0, oldSize).Contains((int)e.ParentGroupId));

            while (_childGroups.Any())
            {
                groupsIds.AddRange(_childGroups.Select(e => e.GroupId).ToList());

                startIndex = endIndex;
                endIndex += _childGroups.Count();

                _childGroups = _groupRepo
                    .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(startIndex, endIndex - startIndex).Contains((int)e.ParentGroupId));
            }

            return groupsIds;
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
        [EnableQuery(MaxExpansionDepth = 3)]
        [HttpGet]
        public async Task<IActionResult> GetMyCourseEditions([FromODataUri] int Frequency)
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
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var courseDurationMinutes = _settings.CourseDurationMinutes;
                var totalMinutes = Frequency * courseDurationMinutes;
                
                var _courseEditions = _courseEditionRepo
                    .Get(e => e.Coordinators.Any(e => e.CoordinatorId == userId) 
                        && Math.Ceiling(e.Course.UnitsMinutes / (courseDurationMinutes * 1.0) - e.SchedulePositions.Count) >= Frequency) 
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

        [Authorize(Policy = "Coordinator")]
        [EnableQuery(MaxExpansionDepth = 3)]
        [HttpGet]
        [ODataRoute("({key1},{key2})/GetMyCourseEdition(Frequency={Frequency})")]
        public async Task<IActionResult> GetMyCourseEdition([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] double Frequency)
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
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var courseDurationMinutes = _settings.CourseDurationMinutes;
                var totalMinutes = Frequency * courseDurationMinutes;

                var _courseEditions = _courseEditionRepo
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2
                        && e.Coordinators.Any(e => e.CoordinatorId == userId)
                        && Math.Ceiling(e.Course.UnitsMinutes / (courseDurationMinutes * 1.0) - e.SchedulePositions.Count) >= Frequency)
                    .Include(e => e.SchedulePositions)
                    .Include(e => e.Course)
                    .Include(e => e.Coordinators);

                return Ok(SingleResult.Create(_courseEditions));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2})/GetBusyPeriods(Weeks={Weeks})")]
        public async Task<IActionResult> GetBusyPeriods([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _courseEdition = _courseEditionRepo
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2)
                    .Include(e => e.Coordinators)
                    .Include(e => e.Groups)
                        .ThenInclude(e => e.Group);

                if (!_courseEdition.Any())
                {
                    return NotFound();
                }

                var courseEdition = await _courseEdition.FirstOrDefaultAsync();

                var coordinatorsIds = courseEdition.Coordinators.Select(e => e.CoordinatorId).ToList();

                var groupsIds = GetNestedGroupsIds(courseEdition, _groupRepo);

                var _timestamps = _schedulePositionRepo
                    .Get(e => Weeks.Contains(e.CourseRoomTimestamp.Timestamp.Week) 
                              && (e.CourseEdition.Coordinators.Select(e => e.CoordinatorId).Any(e => coordinatorsIds.Contains(e))
                              || e.CourseEdition.Groups.Select(e => e.GroupId).Any(e => groupsIds.Contains(e))))
                    .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Coordinators)
                    .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Groups)
                    .Include(e => e.CourseRoomTimestamp)
                        .ThenInclude(e => e.Timestamp)
                    .Select(e => e.CourseRoomTimestamp.Timestamp);


                return Ok(_timestamps.Any() ? _timestamps : Enumerable.Empty<Timestamp>().AsQueryable());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2})/IsPeriodBusy(PeriodIndex={PeriodIndex},Day={Day},Weeks={Weeks})")]
        public async Task<IActionResult> IsPeriodBusy([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int PeriodIndex, [FromODataUri] int Day, [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _courseEdition = _courseEditionRepo
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2)
                    .Include(e => e.Coordinators)
                    .Include(e => e.Groups)
                    .ThenInclude(e => e.Group);

                if (!_courseEdition.Any())
                {
                    return NotFound();
                }

                var courseEdition = await _courseEdition.FirstOrDefaultAsync();

                var coordinatorsIds = courseEdition.Coordinators.Select(e => e.CoordinatorId).ToList();

                var groupsIds = GetNestedGroupsIds(courseEdition, _groupRepo);

                var _timestamps = _schedulePositionRepo
                    .Get(e => e.CourseRoomTimestamp.Timestamp.PeriodIndex == PeriodIndex
                              && e.CourseRoomTimestamp.Timestamp.Day == Day
                              && Weeks.Contains(e.CourseRoomTimestamp.Timestamp.Week)
                              && e.CourseRoomTimestamp.Timestamp.PeriodIndex == PeriodIndex &&
                              e.CourseRoomTimestamp.Timestamp.Day == Day
                              && (e.CourseEdition.Coordinators.Select(e => e.CoordinatorId)
                                      .Any(e => coordinatorsIds.Contains(e))
                                  || e.CourseEdition.Groups.Select(e => e.GroupId).Any(e => groupsIds.Contains(e))))
                    .Include(e => e.CourseEdition)
                    .ThenInclude(e => e.Coordinators)
                    .Include(e => e.CourseEdition)
                    .ThenInclude(e => e.Groups)
                    .Include(e => e.CourseRoomTimestamp)
                    .ThenInclude(e => e.Timestamp);

                return Ok(_timestamps.Any());
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
