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
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Coordinators")]
    public class CoordinatorsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public CoordinatorsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateCoordinator([FromBody] CoordinatorDto coordinatorDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _coordinator = await _unitOfWork.Coordinators.Add(coordinatorDto.FromDto());

                if (_coordinator != null)
                {
                    await _unitOfWork.CompleteAsync();
                    return Created(_coordinator);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetCoordinators()
        {
            return Ok(_unitOfWork.Coordinators.GetAll());
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetCoordinator([FromODataUri] int key)
        {
            try
            {
                var _coordinator = _unitOfWork.Coordinators.Get(e => e.UserId == key);
                if (!_coordinator.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_coordinator));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("({key})")]
        public async Task<IActionResult> UpdateCoordinator([FromODataUri] int key, [FromBody] Delta<Coordinator> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _coordinator = await _unitOfWork.Coordinators.GetFirst(e => e.UserId == key);
                if (_coordinator == null)
                {
                    return NotFound();
                }

                delta.Patch(_coordinator);

                await _unitOfWork.CompleteAsync();

                return Ok(_coordinator);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteCoordinator([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.Coordinators.Delete(e => e.UserId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                var courseEditions = await _unitOfWork.CoordinatorCourseEditions
                    .Get(e => e.CoordinatorId == key).FirstOrDefaultAsync();

                if (courseEditions != null)
                {
                    return BadRequest("You cannot remove coordinator role from this user because there are some course editions assigned to him.");
                }

                await _unitOfWork.CompleteAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
