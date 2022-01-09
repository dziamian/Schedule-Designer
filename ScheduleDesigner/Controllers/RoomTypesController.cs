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

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateRoomType([FromBody] RoomType roomType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _roomType = await _unitOfWork.RoomTypes.Add(roomType);

                if (_roomType == null)
                {
                    return NotFound();
                }

                await _unitOfWork.CompleteAsync();
                return Created(_roomType);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [CustomEnableQuery(PageSize = 20)]
        [ODataRoute("")]
        public IActionResult GetRoomTypes()
        {
            return Ok(_unitOfWork.RoomTypes.GetAll());
        }

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
                return BadRequest(e.Message);
            }
        }

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
                return BadRequest(e.Message);
            }
        }

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
                return BadRequest(e.Message);
            }
        }
    }
}
