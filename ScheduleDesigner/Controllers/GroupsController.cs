using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Attributes;
using System.Threading;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Groups")]
    public class GroupsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public GroupsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateGroup([FromBody] Group group)
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
                    if (group.ParentGroupId != null)
                    {
                        var parentGroup = _unitOfWork.Groups
                            .Get(e => e.GroupId == group.ParentGroupId)
                            .FirstOrDefault();

                        if (parentGroup == null) 
                        {
                            return BadRequest("Could not find parent group you have chosen.");
                        }
                    }

                    var _group = await _unitOfWork.Groups.Add(group);

                    if (_group != null)
                    {
                        await _unitOfWork.CompleteAsync();
                        return Created(_group);
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
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [CustomEnableQuery(PageSize = 20)]
        [ODataRoute("")]
        public IActionResult GetGroups()
        {
            return Ok(_unitOfWork.Groups.GetAll());
        }

        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetGroup([FromODataUri] int key)
        {
            try
            {
                var _group = _unitOfWork.Groups
                    .Get(e => e.GroupId == key);
                if (!_group.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_group));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupFullName([FromODataUri] int key)
        {
            try
            {
                var _group = _unitOfWork.Groups
                    .Get(e => e.GroupId == key)
                    .Include(e => e.ParentGroup);

                if (!_group.Any())
                {
                    return NotFound();
                }

                var group = await _group.FirstAsync();
                var fullGroupName = group.Name;
                var groupIds = new List<int>() { group.GroupId };
                var levels = 0;

                while (group.ParentGroupId != null)
                {
                    ++levels;
                    _group = _group.ThenInclude(e => e.ParentGroup);
                    group = await _group.FirstAsync();
                    for (var i = 0; i < levels; ++i)
                    {
                        group = group.ParentGroup;
                    }
                    fullGroupName = group.Name + fullGroupName;
                    groupIds.Add(group.GroupId);
                }

                return Ok(new GroupFullName { FullName = fullGroupName, GroupsIds = groupIds, Levels = levels });
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupsFullNames([FromODataUri] IEnumerable<int> GroupsIds)
        {
            try
            {
                var groupsFullNamesList = new List<GroupFullName>();
                var groupsFullNamesDictionary = new Dictionary<int, GroupFullName>();

                foreach (var _groupId in GroupsIds)
                {
                    if (groupsFullNamesDictionary.TryGetValue(_groupId, out var groupFullName))
                    {
                        groupsFullNamesList.Add(groupFullName);
                        continue;
                    }

                    var _group = _unitOfWork.Groups
                    .Get(e => e.GroupId == _groupId)
                    .Include(e => e.ParentGroup);

                    if (!_group.Any())
                    {
                        return NotFound();
                    }

                    var group = await _group.FirstAsync();
                    var fullGroupName = group.Name;
                    var groupIds = new List<int>() { group.GroupId };
                    var levels = 0;

                    while (group.ParentGroupId != null)
                    {
                        ++levels;
                        _group = _group.ThenInclude(e => e.ParentGroup);
                        group = await _group.FirstAsync();
                        for (var i = 0; i < levels; ++i)
                        {
                            group = group.ParentGroup;
                        }
                        fullGroupName = group.Name + fullGroupName;
                        groupIds.Add(group.GroupId);
                    }

                    groupFullName = new GroupFullName {FullName = fullGroupName, GroupsIds = groupIds, Levels = levels};
                    groupsFullNamesDictionary.Add(groupIds[0], groupFullName);
                    groupsFullNamesList.Add(groupFullName);
                }

                return Ok(groupsFullNamesList);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("({key})")]
        public IActionResult UpdateGroup([FromODataUri] int key, [FromBody] Delta<Group> delta, [FromQuery] string connectionId)
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
                    var originGroup = _unitOfWork.Groups
                    .GetFirst(e => e.GroupId == key).Result;
                    if (originGroup == null)
                    {
                        return NotFound();
                    }

                    if (!delta.GetChangedPropertyNames().Contains("ParentGroupId"))
                    {
                        delta.Patch(originGroup);

                        _unitOfWork.Complete();

                        return Ok(originGroup);
                    }

                    var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);
                    if (connectionId == null)
                    {
                        return BadRequest("Connection id not found.");
                    }

                    delta.TryGetPropertyValue("ParentGroupId", out var destinationGroupIdObject);
                    var destinationGroupId = (int) destinationGroupIdObject;

                    var destinationGroup = _unitOfWork.Groups.Get(e => e.GroupId == destinationGroupId).FirstOrDefault();
                    if (destinationGroup == null)
                    {
                        return BadRequest("Could not find destination group.");
                    }

                    var childGroupsIds = Methods.GetChildGroups(new List<Group>() { originGroup }, _unitOfWork.Groups);
                    var parentGroupsIds = Methods.GetParentGroups(new List<Group>() { destinationGroup }, _unitOfWork.Groups);

                    if (childGroupsIds.Contains(destinationGroupId))
                    {
                        return BadRequest("Destination group cannot be the child of origin group.");
                    }

                    var groupsIds = childGroupsIds.Union(parentGroupsIds).ToList();

                    var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

                    var originCourseEditionKeys = _unitOfWork.GroupCourseEditions
                        .Get(e => childGroupsIds.Contains(e.GroupId)).Select(e => new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }).ToList();

                    var destinationCourseEditionKeys = _unitOfWork.GroupCourseEditions
                        .Get(e => parentGroupsIds.Contains(e.GroupId)).Select(e => new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }).ToList();

                    if (originCourseEditionKeys.Intersect(destinationCourseEditionKeys).Any())
                    {
                        return BadRequest("Origin and destination groups have common course editions.");
                    }

                    var courseEditionKeys = originCourseEditionKeys.Union(destinationCourseEditionKeys).ToList();

                    lock (ScheduleHub.CourseEditionLocks)
                    {
                        ScheduleHub.AddCourseEditionsLocks(courseEditionKeys, ref courseEditionQueues);
                    }

                    ScheduleHub.EnterQueues(courseEditionQueues.Values);
                    try
                    {
                        var notLockedCourseEditions = _unitOfWork.GroupCourseEditions
                            .Get(e => groupsIds.Contains(e.GroupId)
                                && (e.CourseEdition.LockUserId != userId || e.CourseEdition.LockUserConnectionId != connectionId))
                            .Include(e => e.CourseEdition)
                            .ToList();

                        if (notLockedCourseEditions.Any())
                        {
                            return BadRequest("You did not lock all groups course editions.");
                        }

                        var courseEditionIds = courseEditionKeys.Select(e => e.CourseEditionId).ToList();

                        var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
                        var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

                        var schedulePositionKeys = _unitOfWork.SchedulePositions
                        .Get(e => courseEditionIds.Contains(e.CourseEditionId)).Select(e => new SchedulePositionKey
                        {
                            TimestampId = e.TimestampId,
                            RoomId = e.RoomId
                        }).ToList();

                        lock (ScheduleHub.SchedulePositionLocksL1)
                        lock (ScheduleHub.SchedulePositionLocksL2)
                        {
                            ScheduleHub.AddSchedulePositionsLocksL1(schedulePositionKeys, ref schedulePositionQueuesL1);
                            ScheduleHub.AddSchedulePositionsLocksL2(schedulePositionKeys, ref schedulePositionQueuesL2);
                        }

                        ScheduleHub.EnterQueues(schedulePositionQueuesL1.Values);
                        ScheduleHub.EnterQueues(schedulePositionQueuesL2.Values);
                        try
                        {
                            var notLockedSchedulePositions = _unitOfWork.SchedulePositions
                                .Get(e => courseEditionIds.Contains(e.CourseEditionId)
                                    && (e.LockUserId != userId || e.LockUserConnectionId != connectionId))
                                .ToList();

                            if (notLockedSchedulePositions.Any())
                            {
                                return BadRequest("You did not lock all required positions in schedule.");
                            }

                            //no conflicts
                            var originCourseEditionIds = originCourseEditionKeys.Select(e => e.CourseEditionId).ToList();
                            var originTimestamps = _unitOfWork.SchedulePositions
                                .Get(e => originCourseEditionIds.Contains(e.CourseEditionId))
                                .Include(e => e.Timestamp)
                                .Select(e => e.Timestamp)
                                .ToList();

                            var destinationCourseEditionIds = destinationCourseEditionKeys.Select(e => e.CourseEditionId).ToList();
                            var destinationTimestamps = _unitOfWork.SchedulePositions
                                .Get(e => destinationCourseEditionIds.Contains(e.CourseEditionId))
                                .Include(e => e.Timestamp)
                                .Select(e => e.Timestamp)
                                .ToList();

                            var intersectedTimestamps = originTimestamps.Intersect(destinationTimestamps).ToList();
                            if (intersectedTimestamps.Any())
                            {
                                var timestamps = string.Join(", ", intersectedTimestamps);
                                return BadRequest($"Conflict detected for Timestamps ({timestamps}).");
                            }

                            //update
                            delta.Patch(originGroup);

                            _unitOfWork.Complete();

                            return Ok(originGroup);
                        }
                        finally
                        {
                            ScheduleHub.RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                            ScheduleHub.RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                            ScheduleHub.ExitQueues(schedulePositionQueuesL2.Values);
                            ScheduleHub.ExitQueues(schedulePositionQueuesL1.Values);
                        }
                    }
                    finally
                    {
                        ScheduleHub.RemoveCourseEditionsLocks(courseEditionQueues);
                        ScheduleHub.ExitQueues(courseEditionQueues.Values);
                    }
                }
                finally
                {
                    Monitor.Exit(SchedulePositionsController.ScheduleLock);
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteGroup([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.Groups
                    .Delete(e => e.GroupId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                await _unitOfWork.CompleteAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
