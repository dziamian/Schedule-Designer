using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("StudentGroups")]
    public class StudentGroupsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public StudentGroupsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        public IActionResult CreateStudentGroup([FromBody] StudentGroupDto studentGroupDto)
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
                    var user = _unitOfWork.Users
                        .GetFirst(e => e.UserId == studentGroupDto.StudentId).Result;

                    if (user == null || !user.IsStudent)
                    {
                        return BadRequest("Could not find user with given ID or user is not a student.");
                    }

                    var _student = _unitOfWork.StudentGroups.Add(studentGroupDto.FromDto()).Result;

                    if (_student != null)
                    {
                        _unitOfWork.Complete();
                        return Created(_student);
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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetStudentGroups()
        {
            return Ok(_unitOfWork.StudentGroups.GetAll());
        }
        
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key1},{key2})")]
        public IActionResult GetStudentGroup([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _studentGroup = _unitOfWork.StudentGroups.Get(e => e.GroupId == key1 && e.StudentId == key2);
                if (!_studentGroup.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_studentGroup));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [HttpGet]
        public IActionResult GetMyGroups()
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);

                var _myGroups = _unitOfWork.StudentGroups
                    .Get(e => e.StudentId == userId)
                    .Include(e => e.Group)
                    .Select(e => e.Group)
                    .ToList();

                var parentGroupIds = Methods.GetParentGroups(_myGroups, _unitOfWork.Groups).Distinct();

                var allMyGroups = _unitOfWork.Groups
                    .Get(e => parentGroupIds.Contains(e.GroupId))
                    .ToList();

                return Ok(_myGroups);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
        [CustomEnableQuery]
        public IActionResult GetGroupsStudents([FromODataUri] IEnumerable<int> GroupsIds)
        {
            try
            {
                if (!GroupsIds.Any())
                {
                    return Ok(Enumerable.Empty<User>());
                }

                var mainGroupId = GroupsIds.First();
                var otherGroupIds = GroupsIds.Except(new List<int> { mainGroupId });
                var studentIds = _unitOfWork.StudentGroups
                    .Get(e => GroupsIds.Contains(e.GroupId))
                    .GroupBy(e => e.StudentId)
                    .Select(e => e.Key)
                    .ToList();

                var _students = _unitOfWork.Users
                    .Get(e => studentIds.Contains(e.UserId))
                    .ToList();

                return Ok(_students);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        public IActionResult GiveOrRemoveRepresentativeRole(ODataActionParameters parameters)
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
                    int userId = (int)parameters["UserId"];
                    int groupId = (int)parameters["GroupId"];
                    bool role = (bool)parameters["Role"];

                    var user = _unitOfWork.Users
                        .GetFirst(e => e.UserId == userId).Result;

                    if (user == null || !user.IsStudent)
                    {
                        return BadRequest("Could not find user with given ID or user is not a student.");
                    }

                    var studentGroup = _unitOfWork.StudentGroups
                        .Get(e => e.StudentId == userId && e.GroupId == groupId)
                        .FirstOrDefault();

                    if (studentGroup == null && !role)
                    {
                        return Ok();
                    }
                    if (studentGroup == null)
                    {
                        var assign = new StudentGroup { GroupId = groupId, StudentId = userId, IsRepresentative = true };
                        var result = _unitOfWork.StudentGroups.Add(assign).Result;
                        
                        _unitOfWork.Complete();
                        
                        return Ok();
                    }
                    else
                    {
                        studentGroup.IsRepresentative = role;
                        _unitOfWork.StudentGroups.Update(studentGroup);
                         
                        _unitOfWork.Complete();
                        
                        return Ok();
                    }
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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("({key1},{key2})")]
        public IActionResult UpdateStudentGroup([FromODataUri] int key1, [FromODataUri] int key2, [FromBody] Delta<StudentGroup> delta)
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
                    var user = _unitOfWork.Users
                        .GetFirst(e => e.UserId == key2).Result;

                    if (user == null || !user.IsStudent)
                    {
                        return BadRequest("Could not find user with given ID or user is not a student.");
                    }

                    var _studentGroup = _unitOfWork.StudentGroups.GetFirst(e => e.GroupId == key1 && e.StudentId == key2).Result;
                    if (_studentGroup == null)
                    {
                        return NotFound();
                    }

                    delta.Patch(_studentGroup);

                    _unitOfWork.Complete();

                    return Ok(_studentGroup);
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
        
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> DeleteStudentGroup([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var result = await _unitOfWork.StudentGroups
                    .Delete(e => e.GroupId == key1 && e.StudentId == key2);
                if (result < 0)
                {
                    return NotFound();
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
        public IActionResult ClearStudentGroups()
        {
            try
            {
                int studentGroupsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [StudentGroups]");

                return Ok(new {StudentGroupsAffected = studentGroupsAffected });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
