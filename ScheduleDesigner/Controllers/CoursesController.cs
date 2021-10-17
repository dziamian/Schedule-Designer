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
    [ODataRoutePrefix("Courses")]
    public class CoursesController : ODataController
    {
        private readonly ICourseRepo _courseRepo;
        private readonly ISettingsRepo _settingsRepo;

        public CoursesController(ICourseRepo courseRepo, ISettingsRepo settingsRepo)
        {
            _courseRepo = courseRepo;
            _settingsRepo = settingsRepo;
        }

        private static bool IsDataValid(Course course, Settings settings)
        {
            return (course.UnitsMinutes % settings.CourseDurationMinutes == 0) || (course.UnitsMinutes * 2 / settings.CourseDurationMinutes % settings.TermDurationWeeks == 0);
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateCourse([FromBody] Course course)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var _settings = await _settingsRepo.GetSettings();
            if (_settings == null)
            {
                return BadRequest("Application settings has not been specified.");
            }

            if (!IsDataValid(course, await _settingsRepo.GetSettings()))
            {
                ModelState.AddModelError("CourseUnitsMinutes", "Couldn't calculate the valid amount of courses in term.");
                return BadRequest(ModelState);
            }

            try
            {
                var _course = await _courseRepo.Add(course);

                if (_course != null)
                {
                    return Created(_course);
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
        public IActionResult GetCourses()
        {
            return Ok(_courseRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2})")]
        public IActionResult GetCourse([FromODataUri] int key1)
        {
            try
            {
                var _course = _courseRepo.Get(e => e.CourseId == key1);
                if (!_course.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_course));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> UpdateCourse([FromODataUri] int key1, [FromBody] Delta<Course> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _course = await _courseRepo.GetFirst(e => e.CourseId == key1);
                if (_course == null)
                {
                    return NotFound();
                }

                delta.Patch(_course);

                var _settings = await _settingsRepo.GetSettings();
                if (!IsDataValid(_course, _settings))
                {
                    ModelState.AddModelError("CourseUnitsMinutes", "Couldn't calculate the valid amount of courses in term.");
                    return BadRequest(ModelState);
                }

                await _courseRepo.SaveChanges();

                return Ok(_course);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> DeleteCourse([FromODataUri] int key1)
        {
            try
            {
                var result = await _courseRepo.Delete(e => e.CourseId == key1);
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
