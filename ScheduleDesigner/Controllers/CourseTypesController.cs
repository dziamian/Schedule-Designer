using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Attributes;
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

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateCourseType([FromBody] CourseType courseType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseType = await _unitOfWork.CourseTypes.Add(courseType);

                if (_courseType != null)
                {
                    await _unitOfWork.CompleteAsync();
                    return Created(_courseType);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [CustomEnableQuery(PageSize = 20)]
        [ODataRoute("")]
        public IActionResult GetCourseTypes()
        {
            return Ok(_unitOfWork.CourseTypes.GetAll());
        }

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
                return BadRequest(e.Message);
            }
        }

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
                return BadRequest(e.Message);
            }
        }

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
