using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("ScheduledMoves")]
    public class ScheduledMovesController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public ScheduledMovesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery(MaxExpansionDepth = 3)]
        [ODataRoute("")]
        public IActionResult GetScheduledMoves()
        {
            return Ok(_unitOfWork.ScheduledMoves.GetAll());
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetScheduledMove([FromODataUri] int key)
        {
            try
            {
                var _scheduledMove = _unitOfWork.ScheduledMoves.Get(e => e.MoveId == key);
                if (!_scheduledMove.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_scheduledMove));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize]
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

                    var _scheduledMove = _unitOfWork.ScheduledMoves
                        .Get(e => e.MoveId == _moveId)
                        .Include(e => e.ScheduledPositions);

                    if (!_scheduledMove.Any())
                    {
                        return NotFound();
                    }

                    var scheduledMove = _scheduledMove.FirstOrDefault();
                    if (scheduledMove == null)
                    {
                        return NotFound();
                    }

                    var sourceTimestamps = scheduledMove.ScheduledPositions.Select(e => e.TimestampId_1).ToList();
                    var isConfirmed = scheduledMove.IsConfirmed;
                    var userId = scheduledMove.UserId;
                    var destRoomId = scheduledMove.ScheduledPositions.FirstOrDefault().RoomId_2;
                    var scheduleOrder = scheduledMove.ScheduleOrder;
                    var destTimestamps = scheduledMove.ScheduledPositions.Select(e => e.TimestampId_2).ToList();

                    var _sourceTimestamps = _unitOfWork.Timestamps
                        .Get(e => sourceTimestamps.Contains(e.TimestampId));

                    if (!_sourceTimestamps.Any())
                    {
                        return NotFound();
                    }
                    var sourceWeeks = _sourceTimestamps.Select(e => e.Week).OrderBy(e => e).ToList();
                    
                    var _destRoom = _unitOfWork.Rooms
                        .Get(e => e.RoomId == destRoomId);

                    if (!_destRoom.Any())
                    {
                        return NotFound();
                    }
                    var destRoom = _destRoom.FirstOrDefault();

                    var _destTimestamps = _unitOfWork.Timestamps
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
