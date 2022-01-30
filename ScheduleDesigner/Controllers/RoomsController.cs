using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Attributes;
using Microsoft.AspNetCore.Authorization;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Rooms")]
    public class RoomsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public RoomsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateRoom([FromBody] RoomDto roomDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _room = await _unitOfWork.Rooms.Add(roomDto.FromDto());

                if (_room == null)
                {
                    return NotFound();
                }

                await _unitOfWork.CompleteAsync();
                return Created(_room);
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
        public IActionResult GetRooms()
        {
            return Ok(_unitOfWork.Rooms.GetAll());
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetRoom([FromODataUri] int key)
        {
            try
            {
                var _room = _unitOfWork.Rooms.Get(e => e.RoomId == key);
                if (!_room.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_room));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetRoomsNames([FromODataUri] IEnumerable<int> RoomsIds)
        {
            try
            {
                var roomsNamesList = new List<RoomName>();
                var roomsNamesDictionary = new Dictionary<int, RoomName>();

                foreach (var _roomId in RoomsIds)
                {
                    if (roomsNamesDictionary.TryGetValue(_roomId, out var roomName))
                    {
                        roomsNamesList.Add(roomName);
                        continue;
                    }

                    var _room = _unitOfWork.Rooms
                        .Get(e => e.RoomId == _roomId);

                    if (!_room.Any())
                    {
                        return NotFound();
                    }

                    var room = await _room.FirstOrDefaultAsync();

                    roomName = new RoomName {Name = room.Name};
                    roomsNamesDictionary.Add(room.RoomId, roomName);
                    roomsNamesList.Add(roomName);
                }

                return Ok(roomsNamesList);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("({key})")]
        public async Task<IActionResult> UpdateRoom([FromODataUri] int key, [FromBody] Delta<Room> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _room = await _unitOfWork.Rooms.GetFirst(e => e.RoomId == key);
                if (_room == null)
                {
                    return NotFound();
                }

                delta.Patch(_room);

                await _unitOfWork.CompleteAsync();

                return Ok(_room);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteRoom([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.Rooms.Delete(e => e.RoomId == key);
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
        public IActionResult ClearRooms()
        {
            try
            {
                int roomsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [Rooms]");

                return Ok(new { RoomsAffected = roomsAffected });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
