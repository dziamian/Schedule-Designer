using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Coordinators")]
    public class CoordinatorsController : ODataController
    {
        private readonly IUserRepo _userRepo;
        private readonly ICoordinatorRepo _coordinatorRepo;

        public CoordinatorsController(IUserRepo userRepo, ICoordinatorRepo coordinatorRepo)
        {
            _userRepo = userRepo;
            _coordinatorRepo = coordinatorRepo;
        }

        private static bool IsDataValid(User user)
        {
            return user.Student != null || user.Staff != null;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateCoordinator([FromBody] Coordinator coordinator)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _coordinator = await _coordinatorRepo.Add(coordinator);

                if (_coordinator != null)
                {
                    await _coordinatorRepo.SaveChanges();
                    return Created(_coordinator);
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
        public IActionResult GetCoordinators()
        {
            return Ok(_coordinatorRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetCoordinator([FromODataUri] int key)
        {
            try
            {
                var _coordinator = _coordinatorRepo.Get(e => e.UserId == key);
                if (!_coordinator.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_coordinator));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

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
                var _coordinator = await _coordinatorRepo.GetFirst(e => e.UserId == key);
                if (_coordinator == null)
                {
                    return NotFound();
                }

                delta.Patch(_coordinator);

                await _coordinatorRepo.SaveChanges();

                return Ok(_coordinator);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteCoordinator([FromODataUri] int key)
        {
            try
            {
                var _user = _userRepo
                    .Get(e => e.UserId == key)
                    .Include(e => e.Student)
                    .Include(e => e.Coordinator)
                    .Include(e => e.Staff);

                if (!IsDataValid(_user.First()))
                {
                    return BadRequest("You cannot remove the only existing role for this user.");
                }

                var result = await _coordinatorRepo.Delete(e => e.UserId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                await _coordinatorRepo.SaveChanges();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
