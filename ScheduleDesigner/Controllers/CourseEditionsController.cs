using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Hubs.Interfaces;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using static ScheduleDesigner.Helpers;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("CourseEditions")]
    public class CourseEditionsController : ODataController
    {
        private readonly IGroupRepo _groupRepo;
        private readonly ICourseEditionRepo _courseEditionRepo;
        private readonly ISchedulePositionRepo _schedulePositionRepo;
        private readonly ISettingsRepo _settingsRepo;
        private readonly IStudentGroupRepo _studentGroupRepo;

        public CourseEditionsController(IGroupRepo groupRepo, 
            ICourseEditionRepo courseEditionRepo, 
            ISchedulePositionRepo schedulePositionRepo, 
            ISettingsRepo settingsRepo,
            IStudentGroupRepo studentGroupRepo)
        {
            _groupRepo = groupRepo;
            _courseEditionRepo = courseEditionRepo;
            _schedulePositionRepo = schedulePositionRepo;
            _settingsRepo = settingsRepo;
            _studentGroupRepo = studentGroupRepo;
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

        [Authorize]
        [EnableQuery(MaxExpansionDepth = 3)]
        [HttpGet]
        public async Task<IActionResult> GetFilteredCourseEditions(
            [FromODataUri] IEnumerable<int> CoordinatorsIds,
            [FromODataUri] IEnumerable<int> GroupsIds,
            [FromODataUri] int Frequency)
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
                var predicate = PredicateBuilder.New<CourseEdition>();
                if (CoordinatorsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Coordinators.Any(f => CoordinatorsIds.Contains(f.CoordinatorId)));
                }
                if (GroupsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Groups.Any(f => GroupsIds.Contains(f.GroupId)));
                }

                var courseDurationMinutes = _settings.CourseDurationMinutes;
                var totalMinutes = Frequency * courseDurationMinutes;

                var finalPredicate = predicate.And(e => Math.Ceiling(e.Course.UnitsMinutes / (courseDurationMinutes * 1.0) - e.SchedulePositions.Count) >= Frequency);
                
                var _courseEditions = _courseEditionRepo
                    .Get(finalPredicate) 
                    .Include(e => e.SchedulePositions)
                    .Include(e => e.Course)
                    .Include(e => e.Coordinators)
                    .Include(e => e.Groups);
                
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

                var groupsIds = Helpers.GetNestedGroupsIds(courseEdition, _groupRepo);

                var _timestamps = _schedulePositionRepo
                    .Get(e => Weeks.Contains(e.Timestamp.Week) 
                              && (e.CourseEdition.Coordinators.Select(e => e.CoordinatorId).Any(e => coordinatorsIds.Contains(e))
                              || e.CourseEdition.Groups.Select(e => e.GroupId).Any(e => groupsIds.Contains(e))))
                    .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Coordinators)
                    .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Groups)
                    .Include(e => e.Timestamp)
                    .Select(e => e.Timestamp);


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

                var groupsIds = Helpers.GetNestedGroupsIds(courseEdition, _groupRepo);

                var _timestamps = _schedulePositionRepo
                    .Get(e => e.Timestamp.PeriodIndex == PeriodIndex
                              && e.Timestamp.Day == Day
                              && Weeks.Contains(e.Timestamp.Week)
                              && e.Timestamp.PeriodIndex == PeriodIndex &&
                              e.Timestamp.Day == Day
                              && (e.CourseEdition.Coordinators.Select(e => e.CoordinatorId)
                                      .Any(e => coordinatorsIds.Contains(e))
                                  || e.CourseEdition.Groups.Select(e => e.GroupId).Any(e => groupsIds.Contains(e))))
                    .Include(e => e.CourseEdition)
                    .ThenInclude(e => e.Coordinators)
                    .Include(e => e.CourseEdition)
                    .ThenInclude(e => e.Groups)
                    .Include(e => e.Timestamp);

                return Ok(_timestamps.Any());
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [ODataRoute("({key1},{key2})/GetCourseEditionGroupsSize()")]
        public IActionResult GetCourseEditionGroupsSize([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _courseEdition = _courseEditionRepo
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2)
                    .Include(e => e.Groups);

                if (!_courseEdition.Any())
                {
                    return NotFound();
                }
                var courseEdition = _courseEdition.FirstOrDefault();
                var courseEditionGroupsIds = courseEdition.Groups.Select(e => e.GroupId);

                var _groups = _groupRepo
                    .Get(e => courseEditionGroupsIds.Contains(e.GroupId));

                if (!_groups.Any())
                {
                    return NotFound();
                }
                var groupsIds = _groups.Select(e => e.GroupId).ToList();
                var size = groupsIds.Count();


                var _childGroups = _groupRepo
                    .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(0, size).Contains((int)e.ParentGroupId));

                var startIndex = 0;
                var endIndex = groupsIds.Count;

                while (_childGroups.Any())
                {
                    groupsIds.AddRange(_childGroups.Select(e => e.GroupId).ToList());

                    startIndex = endIndex;
                    endIndex += _childGroups.Count();

                    _childGroups = _groupRepo
                        .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(startIndex, endIndex - startIndex).Contains((int)e.ParentGroupId));
                }

                var groupsSize = _studentGroupRepo
                    .Get(e => groupsIds.Contains(e.GroupId))
                    .GroupBy(e => e.StudentId)
                    .Select(e => e.Key).Count();

                return Ok(groupsSize);
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
