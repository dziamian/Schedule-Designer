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
    [ODataRoutePrefix("Programmes")]
    public class ProgrammesController : ODataController
    {
        private readonly IProgrammeRepo _programmeRepo;

        public ProgrammesController(IProgrammeRepo programmeRepo)
        {
            _programmeRepo = programmeRepo;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateProgramme([FromBody] Programme programme)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _programme = await _programmeRepo.Add(programme);
                
                if (_programme != null)
                {
                    return Created(_programme);
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
        public IActionResult GetProgrammes()
        {
            return Ok(_programmeRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetProgramme([FromODataUri] int key)
        {
            try
            {
                var _programme = _programmeRepo.Get(e => e.ProgrammeId == key);
                if (!_programme.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_programme));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key})")]
        public async Task<IActionResult> UpdateProgramme([FromODataUri] int key, [FromBody] Delta<Programme> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _programme = await _programmeRepo.GetFirst(e => e.ProgrammeId == key);
                if(_programme == null)
                {
                    return NotFound();
                }

                delta.Patch(_programme);

                await _programmeRepo.SaveChanges();

                return Ok(_programme);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteProgramme([FromODataUri] int key)
        {
            try
            {
                var result = await _programmeRepo.Delete(e => e.ProgrammeId == key);
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
