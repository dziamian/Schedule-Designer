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
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Staffs")]
    public class StaffsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public StaffsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        private static bool IsDataValid(User user)
        {
            return user.Student != null || user.Coordinator != null;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateStaff([FromBody] Staff staff)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _staff = await _unitOfWork.Staffs.Add(staff);

                if (_staff != null)
                {
                    await _unitOfWork.CompleteAsync();
                    return Created(_staff);
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
        public IActionResult GetStaffs()
        {
            return Ok(_unitOfWork.Staffs.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetStaff([FromODataUri] int key)
        {
            try
            {
                var _staff = _unitOfWork.Staffs.Get(e => e.UserId == key);
                if (!_staff.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_staff));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key})")]
        public async Task<IActionResult> UpdateStaff([FromODataUri] int key, [FromBody] Delta<Staff> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _staff = await _unitOfWork.Staffs.GetFirst(e => e.UserId == key);
                if (_staff == null)
                {
                    return NotFound();
                }

                delta.Patch(_staff);

                await _unitOfWork.CompleteAsync();

                return Ok(_staff);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteStaff([FromODataUri] int key)
        {
            try
            {
                var _user = _unitOfWork.Users
                    .Get(e => e.UserId == key)
                    .Include(e => e.Student)
                    .Include(e => e.Coordinator)
                    .Include(e => e.Staff);

                if (!IsDataValid(_user.First()))
                {
                    return BadRequest("You cannot remove the only existing role for this user.");
                }

                var result = await _unitOfWork.Staffs.Delete(e => e.UserId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                await _unitOfWork.CompleteAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
