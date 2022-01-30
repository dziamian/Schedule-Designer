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
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Dtos;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("RoomTypes")]
    public class RoomTypesController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoomTypesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateRoomType([FromBody] RoomTypeDto roomTypeDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _roomType = await _unitOfWork.RoomTypes.Add(roomTypeDto.FromDto());

                if (_roomType == null)
                {
                    return NotFound();
                }

                await _unitOfWork.CompleteAsync();
                return Created(_roomType);
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
        public IActionResult GetRoomTypes()
        {
            return Ok(_unitOfWork.RoomTypes.GetAll());
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetRoomType([FromODataUri] int key)
        {
            try
            {
                var _roomType = _unitOfWork.RoomTypes.Get(e => e.RoomTypeId == key);
                if (!_roomType.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_roomType));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("({key})")]
        public async Task<IActionResult> UpdateRoomType([FromODataUri] int key, [FromBody] Delta<RoomType> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _roomType = await _unitOfWork.RoomTypes.GetFirst(e => e.RoomTypeId == key);
                if (_roomType == null)
                {
                    return NotFound();
                }

                delta.Patch(_roomType);

                await _unitOfWork.CompleteAsync();

                return Ok(_roomType);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
        
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteRoomType([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.RoomTypes.Delete(e => e.RoomTypeId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                await _unitOfWork.CompleteAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        public IActionResult ClearRoomTypes()
        {
            try
            {
                int roomTypesAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [RoomTypes]");

                return Ok(new { RoomTypesAffected = roomTypesAffected });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
