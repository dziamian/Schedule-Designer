using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("StudentGroups")]
    public class StudentGroupsController : ODataController
    {
        private readonly IStudentGroupRepo _studentGroupRepo;

        public StudentGroupsController(IStudentGroupRepo studentGroupRepo)
        {
            _studentGroupRepo = studentGroupRepo;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateStudentGroup([FromBody] StudentGroup studentGroup)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _student = await _studentGroupRepo.Add(studentGroup);

                if (_student != null)
                {
                    await _studentGroupRepo.SaveChanges();
                    return Created(_student);
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
        public IActionResult GetStudentGroups()
        {
            return Ok(_studentGroupRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2})")]
        public IActionResult GetStudentGroup([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _studentGroup = _studentGroupRepo.Get(e => e.GroupId == key1 && e.StudentId == key2);
                if (!_studentGroup.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_studentGroup));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

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
                var _studentGroup = await _studentGroupRepo.GetFirst(e => e.GroupId == key1 && e.StudentId == key2);
                if (_studentGroup == null)
                {
                    return NotFound();
                }

                delta.Patch(_studentGroup);

                await _studentGroupRepo.SaveChanges();

                return Ok(_studentGroup);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> DeleteStudent([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var result = await _studentGroupRepo
                    .Delete(e => e.GroupId == key1 && e.StudentId == key2);
                if (result < 0)
                {
                    return NotFound();
                }

                await _studentGroupRepo.SaveChanges();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
