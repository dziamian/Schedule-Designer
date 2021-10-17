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
    [ODataRoutePrefix("Classes")]
    public class ClassesController : ODataController
    {
        private readonly IClassRepo _classRepo;

        public ClassesController(IClassRepo classRepo)
        {
            _classRepo = classRepo;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateClass([FromBody] Class _class)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var __class = await _classRepo.Add(_class);

                if (__class != null)
                {
                    return Created(__class);
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
        public IActionResult GetClasses()
        {
            return Ok(_classRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2},{key3})")]
        public IActionResult GetClass([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3)
        {
            try
            {
                var __class = _classRepo.Get(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2 && e.ClassId == key3);
                if (!__class.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(__class));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key1},{key2},{key3})")]
        public async Task<IActionResult> UpdateClass([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3, [FromBody] Delta<Class> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var __class = await _classRepo.GetFirst(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2 && e.ClassId == key3);
                if (__class == null)
                {
                    return NotFound();
                }

                delta.Patch(__class);

                await _classRepo.SaveChanges();

                return Ok(__class);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key1},{key2},{key3})")]
        public async Task<IActionResult> DeleteClass([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3)
        {
            try
            {
                var result = await _classRepo.Delete(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2 && e.ClassId == key3);
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
