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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("CourseTypes")]
    public class CourseTypesController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public CourseTypesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateCourseType([FromBody] CourseTypeDto courseTypeDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseType = await _unitOfWork.CourseTypes.Add(courseTypeDto.FromDto());

                if (_courseType != null)
                {
                    await _unitOfWork.CompleteAsync();
                    return Created(_courseType);
                }
                return NotFound();
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
        public IActionResult GetCourseTypes()
        {
            return Ok(_unitOfWork.CourseTypes.GetAll());
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetCourseType([FromODataUri] int key)
        {
            try
            {
                var _courseType = _unitOfWork.CourseTypes.Get(e => e.CourseTypeId == key);
                if (!_courseType.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_courseType));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("({key})")]
        public async Task<IActionResult> UpdateCourseType([FromODataUri] int key, [FromBody] Delta<CourseType> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseType = await _unitOfWork.CourseTypes.GetFirst(e => e.CourseTypeId == key);
                if (_courseType == null)
                {
                    return NotFound();
                }

                delta.Patch(_courseType);

                await _unitOfWork.CompleteAsync();

                return Ok(_courseType);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteCourseType([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.CourseTypes.Delete(e => e.CourseTypeId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                var courseIds = _unitOfWork.Courses
                    .Get(e => e.CourseTypeId == key)
                    .Select(e => e.CourseId)
                    .ToList();

                var schedulePosition = await _unitOfWork.SchedulePositions
                    .Get(e => courseIds.Contains(e.CourseId)).FirstOrDefaultAsync();

                if (schedulePosition != null)
                {
                    return BadRequest("You cannot remove this course type because it contains some positions in schedule.");
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
        public IActionResult ClearCourseTypes()
        {
            try
            {
                var schedulePositions = _unitOfWork.SchedulePositions.GetAll();
                if (schedulePositions.Any())
                {
                    return BadRequest("You cannot clear course types because there are some positions in schedule assigned to them.");
                }

                int courseTypesAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [CourseTypes]");

                return Ok(new { CourseTypesAffected = courseTypesAffected });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
