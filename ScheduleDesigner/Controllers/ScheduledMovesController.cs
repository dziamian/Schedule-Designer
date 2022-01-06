using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("ScheduledMoves")]
    public class ScheduledMovesController : ODataController
    {
        private readonly IScheduledMoveRepo _scheduledMoveRepo;
        private readonly ITimestampRepo _timestampRepo;
        private readonly IRoomRepo _roomRepo;

        public ScheduledMovesController(IScheduledMoveRepo scheduledMoveRepo, 
            ITimestampRepo timestampRepo,
            IRoomRepo roomRepo)
        {
            _scheduledMoveRepo = scheduledMoveRepo;
            _timestampRepo = timestampRepo;
            _roomRepo = roomRepo;
        }

        [HttpGet]
        [EnableQuery(MaxExpansionDepth = 3)]
        [ODataRoute("")]
        public IActionResult GetScheduledMoves()
        {
            return Ok(_scheduledMoveRepo.GetAll());
        }

        [HttpGet]
        public IActionResult GetConcreteScheduledMoves([FromODataUri] IEnumerable<int> MovesIds)
        {
            try
            {
                var scheduledMovesReadList = new List<ScheduledMoveRead>();
                var scheduledMovesReadDictionary = new Dictionary<int, ScheduledMoveRead>();

                foreach (var _moveId in MovesIds)
                {
                    if (scheduledMovesReadDictionary.TryGetValue(_moveId, out var scheduledMoveRead))
                    {
                        scheduledMovesReadList.Add(scheduledMoveRead);
                        continue;
                    }

                    var _scheduledMove = _scheduledMoveRepo
                        .Get(e => e.MoveId == _moveId);

                    if (!_scheduledMove.Any())
                    {
                        return NotFound();
                    }
                    var sourceTimestamps = _scheduledMove.Select(e => e.TimestampId_1).ToList();
                    var isConfirmed = _scheduledMove.FirstOrDefault().IsConfirmed;
                    var userId = _scheduledMove.FirstOrDefault().UserId;
                    var destRoomId = _scheduledMove.FirstOrDefault().RoomId_2;
                    var scheduleOrder = _scheduledMove.FirstOrDefault().ScheduleOrder;
                    var destTimestamps = _scheduledMove.Select(e => e.TimestampId_2).ToList();

                    var _sourceTimestamps = _timestampRepo
                        .Get(e => sourceTimestamps.Contains(e.TimestampId));

                    if (!_sourceTimestamps.Any())
                    {
                        return NotFound();
                    }
                    var sourceWeeks = _sourceTimestamps.Select(e => e.Week).OrderBy(e => e).ToList();
                    
                    var _destRoom = _roomRepo
                        .Get(e => e.RoomId == destRoomId);

                    if (!_destRoom.Any())
                    {
                        return NotFound();
                    }
                    var destRoom = _destRoom.FirstOrDefault();

                    var _destTimestamps = _timestampRepo
                        .Get(e => destTimestamps.Contains(e.TimestampId));

                    if (!_destTimestamps.Any())
                    {
                        return NotFound();
                    }
                    var destPeriodIndex = _destTimestamps.FirstOrDefault().PeriodIndex;
                    var destDay = _destTimestamps.FirstOrDefault().Day;
                    var destWeeks = _destTimestamps.Select(e => e.Week).OrderBy(e => e).ToList();

                    scheduledMoveRead = new ScheduledMoveRead
                    {
                        MoveId = _moveId,
                        IsConfirmed = isConfirmed,
                        UserId = userId,
                        SourceWeeks = sourceWeeks,
                        DestRoomId = destRoom.RoomId,
                        DestRoomName = destRoom.Name,
                        DestRoomTypeId = destRoom.RoomTypeId,
                        DestPeriodIndex = destPeriodIndex,
                        DestDay = destDay,
                        DestWeeks = destWeeks,
                        ScheduleOrder = scheduleOrder
                    };

                    scheduledMovesReadDictionary.Add(_moveId, scheduledMoveRead);
                    scheduledMovesReadList.Add(scheduledMoveRead);
                }
                scheduledMovesReadList.Sort();

                return Ok(scheduledMovesReadList);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
