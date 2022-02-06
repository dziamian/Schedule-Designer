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
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Hubs.Interfaces;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Dtos;
using System.Threading;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("CourseEditions")]
    public class CourseEditionsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public CourseEditionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        public IActionResult CreateCourseEdition([FromBody] CourseEditionDto courseEditionDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var _courseEdition = _unitOfWork.CourseEditions.Add(courseEditionDto.FromDto()).Result;

                    if (_courseEdition != null)
                    {
                        _unitOfWork.Complete();
                        return Created(_courseEdition);
                    }
                    return NotFound();
                }
                finally
                {
                    Monitor.Exit(SchedulePositionsController.ScheduleLock);
                }
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [CustomEnableQuery(MaxExpansionDepth = 3)]
        [HttpGet]
        public async Task<IActionResult> GetFilteredCourseEditions(
            [FromODataUri] IEnumerable<int> CoordinatorsIds,
            [FromODataUri] IEnumerable<int> GroupsIds,
            [FromODataUri] IEnumerable<int> RoomsIds,
            [FromODataUri] int Frequency)
        {
            var _settings = await _unitOfWork.Settings.GetFirst(e => true);
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
                if (RoomsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Course.Rooms.Any(f => RoomsIds.Contains(f.RoomId)));
                }

                var courseDurationMinutes = _settings.CourseDurationMinutes;
                var totalMinutes = Frequency * courseDurationMinutes;

                var finalPredicate = predicate
                    .And(e => Math.Ceiling(e.Course.UnitsMinutes / (courseDurationMinutes * 1.0) - e.SchedulePositions.Count) >= Frequency);
                
                var _courseEditions = _unitOfWork.CourseEditions
                    .Get(finalPredicate) 
                    .Include(e => e.SchedulePositions)
                    .Include(e => e.Course)
                    .Include(e => e.Coordinators)
                    .Include(e => e.Groups);
                
                return Ok(_courseEditions);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [CustomEnableQuery(MaxExpansionDepth = 3)]
        [HttpGet]
        [ODataRoute("({key1},{key2})/GetFilteredCourseEdition(CoordinatorsIds={CoordinatorsIds},GroupsIds={GroupsIds},RoomsIds={RoomsIds},Frequency={Frequency})")]
        public async Task<IActionResult> GetFilteredCourseEdition(
            [FromODataUri] int key1, [FromODataUri] int key2,
            [FromODataUri] IEnumerable<int> CoordinatorsIds,
            [FromODataUri] IEnumerable<int> GroupsIds,
            [FromODataUri] IEnumerable<int> RoomsIds,
            [FromODataUri] double Frequency)
        {
            var _settings = await _unitOfWork.Settings.GetFirst(e => true);
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
                if (RoomsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Course.Rooms.Any(f => RoomsIds.Contains(f.RoomId)));
                }
                var courseDurationMinutes = _settings.CourseDurationMinutes;
                var totalMinutes = Frequency * courseDurationMinutes;

                var finalPredicate = PredicateBuilder.New<CourseEdition>()
                    .And(e => e.CourseId == key1 && e.CourseEditionId == key2)
                    .And(predicate)
                    .And(e => Math.Ceiling(e.Course.UnitsMinutes / (courseDurationMinutes * 1.0) - e.SchedulePositions.Count) >= Frequency);

                var _courseEditions = _unitOfWork.CourseEditions
                    .Get(finalPredicate)
                    .Include(e => e.SchedulePositions)
                    .Include(e => e.Course)
                        .ThenInclude(e => e.Rooms)
                    .Include(e => e.Coordinators);

                return Ok(SingleResult.Create(_courseEditions));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key1},{key2})/GetBusyPeriods(Weeks={Weeks})")]
        public async Task<IActionResult> GetBusyPeriods([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] IEnumerable<int> Weeks)
        {
            var _settings = await _unitOfWork.Settings.GetFirst(e => true);
            if (_settings == null)
            {
                return BadRequest("Application settings has not been specified.");
            }

            if (Weeks.Any(week => week > _settings.TermDurationWeeks || week <= 0))
            {
                return BadRequest("Weeks are invalid");
            }

            try
            {
                var _courseEdition = _unitOfWork.CourseEditions
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

                var groupsIds = Methods.GetNestedGroupsIds(courseEdition, _unitOfWork.Groups);

                var _timestamps = _unitOfWork.SchedulePositions
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
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key1},{key2})/IsPeriodBusy(PeriodIndex={PeriodIndex},Day={Day},Weeks={Weeks})")]
        public async Task<IActionResult> IsPeriodBusy(
            [FromODataUri] int key1, 
            [FromODataUri] int key2, 
            [FromODataUri] int PeriodIndex, 
            [FromODataUri] int Day, 
            [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _courseEdition = _unitOfWork.CourseEditions
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

                var groupsIds = Methods.GetNestedGroupsIds(courseEdition, _unitOfWork.Groups);

                var _timestamps = _unitOfWork.SchedulePositions
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
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [HttpGet]
        [ODataRoute("({key1},{key2})/GetCourseEditionGroupsSize()")]
        public IActionResult GetCourseEditionGroupsSize([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _courseEdition = _unitOfWork.CourseEditions
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2)
                    .Include(e => e.Groups);

                if (!_courseEdition.Any())
                {
                    return NotFound();
                }
                var courseEdition = _courseEdition.FirstOrDefault();
                var courseEditionGroupsIds = courseEdition.Groups.Select(e => e.GroupId);

                var _groups = _unitOfWork.Groups
                    .Get(e => courseEditionGroupsIds.Contains(e.GroupId));

                if (!_groups.Any())
                {
                    return NotFound();
                }
                var groupsIds = _groups.Select(e => e.GroupId).ToList();
                var size = groupsIds.Count();


                var _childGroups = _unitOfWork.Groups
                    .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(0, size).Contains((int)e.ParentGroupId));

                var startIndex = 0;
                var endIndex = groupsIds.Count;

                while (_childGroups.Any())
                {
                    groupsIds.AddRange(_childGroups.Select(e => e.GroupId).ToList());

                    startIndex = endIndex;
                    endIndex += _childGroups.Count();

                    _childGroups = _unitOfWork.Groups
                        .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(startIndex, endIndex - startIndex).Contains((int)e.ParentGroupId));
                }

                var groupsSize = _unitOfWork.StudentGroups
                    .Get(e => groupsIds.Contains(e.GroupId))
                    .GroupBy(e => e.StudentId)
                    .Select(e => e.Key).Count();

                return Ok(groupsSize);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetCourseEditions()
        {
            return Ok(_unitOfWork.CourseEditions.GetAll());
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key1},{key2})")]
        public IActionResult GetCourseEdition([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _courseEdition = _unitOfWork.CourseEditions
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2);
                if (!_courseEdition.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_courseEdition));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
        
        [Authorize(Policy = "AdministratorOnly")]
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
                var _courseEdition = await _unitOfWork.CourseEditions
                    .GetFirst(e => e.CourseId == key1 && e.CourseEditionId == key2);
                if (_courseEdition == null)
                {
                    return NotFound();
                }

                delta.Patch(_courseEdition);

                await _unitOfWork.CompleteAsync();

                return Ok(_courseEdition);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> DeleteCourseEdition([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var result = await _unitOfWork.CourseEditions
                    .Delete(e => e.CourseId == key1 && e.CourseEditionId == key2);
                if (result < 0)
                {
                    return NotFound();
                }

                var schedulePosition = await _unitOfWork.SchedulePositions
                    .Get(e => e.CourseEditionId == key2).FirstOrDefaultAsync();

                if (schedulePosition != null)
                {
                    return BadRequest("You cannot remove this course edition because it contains some positions in schedule.");
                }

                await _unitOfWork.CompleteAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        public IActionResult ClearCourseEditions()
        {
            try
            {
                var schedulePositions = _unitOfWork.SchedulePositions.GetAll();
                if (schedulePositions.Any())
                {
                    return BadRequest("You cannot clear course editions because there are some positions in schedule assigned to them.");
                }

                int groupCourseEditionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [GroupCourseEditions]");
                int coordinatorCourseEditionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [CoordinatorCourseEditions]");
                int courseEditionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [CourseEditions]");

                return Ok(new
                { 
                    GroupCourseEditionsAffected = groupCourseEditionsAffected,
                    CoordinatorCourseEditionsAffected = coordinatorCourseEditionsAffected,
                    CourseEditionsAffected = courseEditionsAffected
                });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
