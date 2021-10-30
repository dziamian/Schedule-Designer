using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("CourseEditions")]
    public class CourseEditionsController : ODataController
    {
        private readonly ICourseEditionRepo _courseEditionRepo;
        private readonly ISettingsRepo _settingsRepo;

        public CourseEditionsController(ICourseEditionRepo courseEditionRepo, ISettingsRepo settingsRepo)
        {
            _courseEditionRepo = courseEditionRepo;
            _settingsRepo = settingsRepo;
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
        [HttpPost]
        [ODataRoute("({key1},{key2})/Service.Lock")]
        public async Task<IActionResult> Lock([FromODataUri] int key1, [FromODataUri] int key2)
        {
            //Console.WriteLine(HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == "user_id"));
            return Ok();
        }

        [Authorize]
        [HttpPost]
        [ODataRoute("({key1},{key2})/Service.Unlock")]
        public async Task<IActionResult> Unlock([FromODataUri] int key1, [FromODataUri] int key2)
        {
            //Console.WriteLine(HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == "user_id"));
            return Ok();
        }

        [Authorize]
        [EnableQuery]
        [HttpGet]
        public async Task<IActionResult> GetMyCourseEditions([FromODataUri] double Frequency)
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
                //var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);
                var userId = 853;

                var courseDurationMinutes = _settings.CourseDurationMinutes;
                var totalMinutes = Frequency * courseDurationMinutes;

                var _courseEditions = _courseEditionRepo
                    .Get(e => e.Coordinators.Any(e => e.CoordinatorId == userId) && e.Course.UnitsMinutes - e.SchedulePositions.Count * courseDurationMinutes >= totalMinutes)
                    .Include(e => e.SchedulePositions)
                    .Include(e => e.Course)
                    .Include(e => e.Coordinators);

                return Ok(_courseEditions);
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
