using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("SchedulePositions")]
    public class SchedulePositionsController : ODataController
    {
        private readonly IRoomRepo _roomRepo;
        private readonly ISchedulePositionRepo _schedulePositionRepo;

        public SchedulePositionsController(IRoomRepo roomRepo, ISchedulePositionRepo schedulePositionRepo)
        {
            _roomRepo = roomRepo;
            _schedulePositionRepo = schedulePositionRepo;
        }

        [Authorize(Policy = "Coordinator")]
        [HttpGet]
        [EnableQuery(MaxExpansionDepth = 4)]
        public IActionResult GetScheduleAsCoordinator([FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _schedulePositions = _schedulePositionRepo
                    .Get(e => e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId)
                              && Weeks.Contains(e.CourseRoomTimestamp.Timestamp.Week))
                    .Include(e => e.CourseEdition)
                    .ThenInclude(e => e.Coordinators)
                    .Include(e => e.CourseRoomTimestamp)
                    .ThenInclude(e => e.Timestamp);

                return Ok(_schedulePositions);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            } 
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("Service.GetRoomsAvailability(RoomsIds={RoomsIds},PeriodIndex={PeriodIndex},Day={Day},Weeks={Weeks})")]
        public IActionResult GetRoomsAvailibility([FromODataUri] IEnumerable<int> RoomsIds, [FromODataUri] int PeriodIndex, [FromODataUri] int Day, [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _rooms = _roomRepo
                    .Get(e => RoomsIds.Contains(e.RoomId))
                    .Select(e => e.RoomId);

                var rooms = new Dictionary<int, RoomAvailability>();
                foreach (var _room in _rooms)
                {
                    rooms.TryAdd(_room, new RoomAvailability {RoomId = _room, IsBusy = false});
                }

                var _schedulePositions = _schedulePositionRepo
                    .Get(e => _rooms.Contains(e.RoomId) && e.CourseRoomTimestamp.Timestamp.PeriodIndex == PeriodIndex 
                        && e.CourseRoomTimestamp.Timestamp.Day == Day && Weeks.Contains(e.CourseRoomTimestamp.Timestamp.Week))
                    .Include(e => e.CourseRoomTimestamp)
                        .ThenInclude(e => e.Timestamp)
                    .GroupBy(e => e.RoomId)
                    .Select(e => new RoomAvailability {RoomId = e.Key});

                foreach (var schedulePosition in _schedulePositions)
                {
                    var updatedRoom = rooms[schedulePosition.RoomId];
                    updatedRoom.IsBusy = true;
                    rooms[schedulePosition.RoomId] = updatedRoom;
                }

                return Ok(rooms.Values);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
