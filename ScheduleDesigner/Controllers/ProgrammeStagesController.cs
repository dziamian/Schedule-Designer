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
    [ODataRoutePrefix("ProgrammeStages")]
    public class ProgrammeStagesController : ODataController
    {
        private readonly IProgrammeStageRepo _programmeStageRepo;

        public ProgrammeStagesController(IProgrammeStageRepo programmeStageRepo)
        {
            _programmeStageRepo = programmeStageRepo;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateProgrammeStage([FromBody] ProgrammeStage programmeStage)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _programmeStage = await _programmeStageRepo.Add(programmeStage);
                
                if (_programmeStage != null)
                {
                    return Created(_programmeStage);
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
        public IActionResult GetProgrammeStages()
        {
            return Ok(_programmeStageRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2})")]
        public IActionResult GetProgrammeStage([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _programmeStage = _programmeStageRepo.Get(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2);
                if (!_programmeStage.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_programmeStage));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> UpdateProgrammeStage([FromODataUri] int key1, [FromODataUri] int key2, [FromBody] Delta<ProgrammeStage> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _programmeStage = await _programmeStageRepo.GetFirst(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2);
                if (_programmeStage == null)
                {
                    return NotFound();
                }

                delta.Patch(_programmeStage);

                await _programmeStageRepo.SaveChanges();

                return Ok(_programmeStage);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> DeleteProgrammeStage([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var result = await _programmeStageRepo.Delete(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2);
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
