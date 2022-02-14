using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="CoordinatorCourseEdition"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("CoordinatorCourseEditions")]
    public class CoordinatorCourseEditionsController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public CoordinatorCourseEditionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="CoordinatorCourseEdition"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="CoordinatorCourseEdition"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetCoordinatorCourseEditions()
        {
            return Ok(_unitOfWork.CoordinatorCourseEditions.GetAll());
        }

        /// <summary>
        /// Tworzy nowe wystąpienie <see cref="CoordinatorCourseEdition"/>.
        /// </summary>
        /// <param name="coordinatorCourseEditionDto">Obiekt transferu danych</param>
        /// <param name="connectionId">ID połączenia z centrum SignalR</param>
        /// <returns>Nowo utworzone wystąpienie <see cref="CoordinatorCourseEdition"/></returns>
        /// <response code="201">Zwrócono nowo utworzone wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie transferu; 
        /// nie zostało podane ID połączenia; 
        /// plan zajęć jest aktualnie zablokowany;
        /// nie zostały odnalezione wymagane dane z bazy;
        /// nie zostały zablokowane wymagane edycje zajęć w bazie (dotyczące wybranego prowadzącego i przypisywanej edycji zajęć);
        /// nie zostały zablokowane wymagane pozycje harmonogramu w bazie (dotyczące zablokowanych edycji zajęć);
        /// wykryty został konflikt w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie udało się dodać nowo utworzonego wystąpienia do bazy danych</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult CreateCoordinatorCourseEdition([FromBody] CoordinatorCourseEditionDto coordinatorCourseEditionDto, [FromQuery] string connectionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (connectionId == null)
            {
                return BadRequest("Connection id not found.");
            }

            try
            {
                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                    var user = _unitOfWork.Users.GetFirst(e => e.UserId == coordinatorCourseEditionDto.CoordinatorId).Result;

                    if (user == null || !user.IsCoordinator)
                    {
                        return BadRequest("Could not find user with given ID or user is not a coordinator.");
                    }

                    var currentCourseEdition = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == coordinatorCourseEditionDto.CourseId 
                        && e.CourseEditionId == coordinatorCourseEditionDto.CourseEditionId)
                        .Include(e => e.Coordinators)
                        .FirstOrDefault();

                    if (currentCourseEdition == null)
                    {
                        return BadRequest("Course edition not found.");
                    }

                    if (currentCourseEdition.Coordinators.Any(e => e.CoordinatorId == coordinatorCourseEditionDto.CoordinatorId))
                    {
                        return BadRequest("This coordinator is already assigned.");
                    }

                    var coordinatorCourseEditions = _unitOfWork.CoordinatorCourseEditions
                        .Get(e => e.CoordinatorId == coordinatorCourseEditionDto.CoordinatorId)
                        .Include(e => e.CourseEdition)
                        .Select(e => e.CourseEdition)
                        .ToList();

                    coordinatorCourseEditions.Add(currentCourseEdition);

                    
                    var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

                    var courseEditionKeys = coordinatorCourseEditions.Select(e => new CourseEditionKey
                    {
                        CourseId = e.CourseId,
                        CourseEditionId = e.CourseEditionId
                    }).ToList();

                    lock (ScheduleHub.CourseEditionLocks)
                    {
                        ScheduleHub.AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                    }

                    ScheduleHub.EnterQueues(courseEditionQueues.Values);
                    try
                    {
                        var notLockedCurrentCourseEdition = _unitOfWork.CourseEditions
                            .Get(e => e.CourseId == coordinatorCourseEditionDto.CourseId && e.CourseEditionId == coordinatorCourseEditionDto.CourseEditionId
                                && (e.LockUserId != userId || e.LockUserConnectionId != connectionId))
                            .ToList();

                        if (notLockedCurrentCourseEdition.Any()) 
                        {
                            return BadRequest("You did not lock chosen course edition.");
                        }

                        var notLockedCourseEditions = _unitOfWork.CoordinatorCourseEditions
                            .Get(e => e.CoordinatorId == coordinatorCourseEditionDto.CoordinatorId 
                                && (e.CourseEdition.LockUserId != userId || e.CourseEdition.LockUserConnectionId != connectionId))
                            .Include(e => e.CourseEdition)
                            .ToList();

                        if (notLockedCourseEditions.Any())
                        {
                            return BadRequest("You did not lock all coordinator course editions.");
                        }

                        var courseEditionIds = courseEditionKeys.Select(e => e.CourseEditionId).ToList();
                        
                        var schedulePositionQueuesL1 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();
                        var schedulePositionQueuesL2 = new SortedList<SchedulePositionKey, ConcurrentQueue<object>>();

                        var schedulePositionKeys = _unitOfWork.SchedulePositions
                        .Get(e => courseEditionIds.Contains(e.CourseEditionId)).Select(e => new SchedulePositionKey
                        {
                            TimestampId = e.TimestampId,
                            RoomId = e.RoomId
                        }).ToList();

                        lock (ScheduleHub.SchedulePositionLocksL1)
                        lock (ScheduleHub.SchedulePositionLocksL2)
                        {
                            ScheduleHub.AddSchedulePositionsLocksL1(schedulePositionKeys, schedulePositionQueuesL1);
                            ScheduleHub.AddSchedulePositionsLocksL2(schedulePositionKeys, schedulePositionQueuesL2);
                        }

                        ScheduleHub.EnterQueues(schedulePositionQueuesL1.Values);
                        ScheduleHub.EnterQueues(schedulePositionQueuesL2.Values);
                        try
                        {
                            var notLockedSchedulePositions = _unitOfWork.SchedulePositions
                                .Get(e => courseEditionIds.Contains(e.CourseEditionId)
                                    && (e.LockUserId != userId || e.LockUserConnectionId != connectionId))
                                .ToList();

                            if (notLockedSchedulePositions.Any())
                            {
                                return BadRequest("You did not lock all required positions in schedule.");
                            }

                            //no conflicts
                            var currentTimestamps = _unitOfWork.SchedulePositions
                                .Get(e => e.CourseEditionId == currentCourseEdition.CourseEditionId)
                                .Include(e => e.Timestamp)
                                .Select(e => e.Timestamp)
                                .ToList();

                            var otherCourseEditionIds = courseEditionIds.Where(e => e != currentCourseEdition.CourseEditionId).ToList();
                            var otherTimestamps = _unitOfWork.SchedulePositions
                                .Get(e => otherCourseEditionIds.Contains(e.CourseEditionId))
                                .Include(e => e.Timestamp)
                                .Select(e => e.Timestamp)
                                .ToList();

                            var intersectedTimestamps = currentTimestamps.Intersect(otherTimestamps).ToList();
                            if (intersectedTimestamps.Any())
                            {
                                var timestamps = string.Join(", ", intersectedTimestamps);
                                return BadRequest($"Conflict detected for Timestamps [PeriodIndex, Day, Week] {timestamps}.");
                            }

                            //add
                            var _coordinatorCourseEdition = _unitOfWork.CoordinatorCourseEditions.Add(coordinatorCourseEditionDto.FromDto()).Result;

                            if (_coordinatorCourseEdition != null)
                            {
                                _unitOfWork.Complete();
                                return Created(_coordinatorCourseEdition);
                            }
                            return NotFound();
                        }
                        finally
                        {
                            ScheduleHub.RemoveSchedulePositionsLocksL2(schedulePositionQueuesL2);
                            ScheduleHub.RemoveSchedulePositionsLocksL1(schedulePositionQueuesL1);
                            ScheduleHub.ExitQueues(schedulePositionQueuesL2.Values);
                            ScheduleHub.ExitQueues(schedulePositionQueuesL1.Values);
                        }
                    }
                    finally
                    {
                        ScheduleHub.RemoveCourseEditionsLocks(courseEditionQueues);
                        ScheduleHub.ExitQueues(courseEditionQueues.Values);
                    }
                }
                finally
                {
                    Monitor.Exit(SchedulePositionsController.ScheduleLock);
                }
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Usuwa pojedyncze istniejące wystąpienie <see cref="CoordinatorCourseEdition"/>.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID edycji zajęć</param>
        /// <param name="key3">ID prowadzącego</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięto wystąpienie</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono wystąpienia, które miało zostać usunięte</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key1},{key2},{key3}")]
        [ODataRoute("({key1},{key2},{key3})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteCoordinatorCourseEdition([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] int key3)
        {
            try
            {
                var result = await _unitOfWork.CoordinatorCourseEditions
                    .Delete(e => e.CourseId == key1 && e.CourseEditionId == key2 && e.CoordinatorId == key3);
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
    }
}
