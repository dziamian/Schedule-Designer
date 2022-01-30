using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Linq;
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
        public async Task<IActionResult> CreateStudentGroup([FromBody] StudentGroupDto studentGroupDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _student = await _unitOfWork.StudentGroups.Add(studentGroupDto.FromDto());

                if (_student != null)
                {
                    await _unitOfWork.CompleteAsync();
                    return Created(_student);
                }
                return NotFound();
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

                return Ok(_myGroups);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> UpdateStudentGroup([FromODataUri] int key1, [FromODataUri] int key2, [FromBody] Delta<StudentGroup> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _studentGroup = await _unitOfWork.StudentGroups.GetFirst(e => e.GroupId == key1 && e.StudentId == key2);
                if (_studentGroup == null)
                {
                    return NotFound();
                }

                delta.Patch(_studentGroup);

                await _unitOfWork.CompleteAsync();

                return Ok(_studentGroup);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
        
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> DeleteStudent([FromODataUri] int key1, [FromODataUri] int key2)
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
