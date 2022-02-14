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
using System;
using System.Collections.Generic;
using System.Linq;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="ScheduledMove"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("ScheduledMoves")]
    public class ScheduledMovesController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public ScheduledMovesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="ScheduledMove"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="ScheduledMove"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery(MaxExpansionDepth = 3)]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetScheduledMoves()
        {
            return Ok(_unitOfWork.ScheduledMoves.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="ScheduledMove"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID zaplanowanego ruchu</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="ScheduledMove"/></returns>
        /// <response code="200">Zwrócono żądane wystąpienie</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize]
        [HttpGet("{key}")]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
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
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca dokładne informacje na temat zaplanowanych ruchów wraz z ich 
        /// przesunięciami w postaci listy obiektów klasy <see cref="ScheduledMoveRead"/>.
        /// </summary>
        /// <param name="MovesIds">Kolekcja identyfikatorów zaplanowanych ruchów w planie</param>
        /// <returns>Listę informacji na temat zaplanowanych ruchów wraz z ich przesunięciami</returns>
        /// <response code="200">Zwrócono listę informacji na temat zaplanowanych ruchów</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono wymaganych danych do zebrania informacji na temat zaplanowanego ruchu</response>
        [Authorize]
        [HttpGet("Service.GetConcreteScheduledMoves({MovesIds})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
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
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
