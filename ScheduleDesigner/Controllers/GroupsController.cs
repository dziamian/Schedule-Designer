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
    [ODataRoutePrefix("Groups")]
    public class GroupsController : ODataController
    {
        private readonly IGroupRepo _groupRepo;

        public GroupsController(IGroupRepo groupRepo)
        {
            _groupRepo = groupRepo;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateGroup([FromBody] Group group)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _group = await _groupRepo.Add(group);

                if (_group != null)
                {
                    return Created(_group);
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
        public IActionResult GetGroups()
        {
            return Ok(_groupRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2},{key3},{key4})")]
        public IActionResult GetGroup([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3, [FromODataUri] int key4)
        {
            try
            {
                var _group = _groupRepo
                    .Get(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2 && e.ClassId == key3 && e.GroupId == key4);
                if (!_group.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_group));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key1},{key2},{key3},{key4})")]
        public async Task<IActionResult> UpdateProgrammeStage([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3, [FromODataUri] int key4, [FromBody] Delta<Group> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _group = await _groupRepo
                    .GetFirst(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2 && e.ClassId == key3 && e.GroupId == key4);
                if (_group == null)
                {
                    return NotFound();
                }

                delta.Patch(_group);

                await _groupRepo.SaveChanges();

                return Ok(_group);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key1},{key2},{key3},{key4})")]
        public async Task<IActionResult> DeleteProgrammeStage([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3, [FromODataUri] int key4)
        {
            try
            {
                var result = await _groupRepo
                    .Delete(e => e.ProgrammeId == key1 && e.ProgrammeStageId == key2 && e.ClassId == key3 && e.GroupId == key4);
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
