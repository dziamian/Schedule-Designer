using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("CourseTypes")]
    public class CourseTypesController : ODataController
    {
        private readonly ICourseTypeRepo _courseTypeRepo;

        public CourseTypesController(ICourseTypeRepo courseTypeRepo)
        {
            _courseTypeRepo = courseTypeRepo;
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
                var _courseType = await _courseTypeRepo.Add(courseType);

                if (_courseType != null)
                {
                    await _courseTypeRepo.SaveChanges();
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
        [EnableQuery(PageSize = 20)]
        [ODataRoute("")]
        public IActionResult GetCourseTypes()
        {
            return Ok(_courseTypeRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetCourseType([FromODataUri] int key)
        {
            try
            {
                var _courseType = _courseTypeRepo.Get(e => e.CourseTypeId == key);
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
                var _courseType = await _courseTypeRepo.GetFirst(e => e.CourseTypeId == key);
                if (_courseType == null)
                {
                    return NotFound();
                }

                delta.Patch(_courseType);

                await _courseTypeRepo.SaveChanges();

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
                var result = await _courseTypeRepo.Delete(e => e.CourseTypeId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                await _courseTypeRepo.SaveChanges();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
