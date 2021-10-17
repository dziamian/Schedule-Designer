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

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("ProgrammeStageCourses")]
    public class ProgrammeStageCoursesController : ODataController
    {
        private readonly IProgrammeStageCourseRepo _programmeStageCourseRepo;

        public ProgrammeStageCoursesController(IProgrammeStageCourseRepo programmeStageCourseRepo)
        {
            _programmeStageCourseRepo = programmeStageCourseRepo;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateProgrammeStageCourse([FromBody] ProgrammeStageCourse programmeStageCourse)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _programmeStageCourse = await _programmeStageCourseRepo.Add(programmeStageCourse);

                if (_programmeStageCourse != null)
                {
                    return Created(_programmeStageCourse);
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
        public IActionResult GetProgrammeStageCourses()
        {
            return Ok(_programmeStageCourseRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2},{key3})")]
        public IActionResult GetProgrameStageCourse([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3)
        {
            try
            {
                var _programmeStageCourse = _programmeStageCourseRepo.Get(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2 && e.CourseId == key3);
                if (!_programmeStageCourse.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_programmeStageCourse));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key1},{key2},{key3})")]
        public async Task<IActionResult> DeleteProgrammeStageCourse([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3)
        {
            try
            {
                var result = await _programmeStageCourseRepo.Delete(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2 && e.CourseId == key3);
                if (result < 0)
                {
                    return NotFound();
                }

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
