using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Helpers;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="SchedulePosition"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("SchedulePositions")]
    public class SchedulePositionsController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Obiekt przeznaczony do blokowania, w przypadku wykonywania krytycznych sekcji operacji na danych powiązanych z planem zajęć.
        /// </summary>
        public static readonly object ScheduleLock = new object();
        
        /// <summary>
        /// Limit czasu oczekiwania na zablokowanie dostępu do obiektu <see cref="ScheduleLock"/>.
        /// </summary>
        public static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public SchedulePositionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="SchedulePosition"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="SchedulePosition"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetSchedulePositions()
        {
            return Ok(_unitOfWork.SchedulePositions.GetAll());
        }

        /// <summary>
        /// Zwraca kolekcję wystąpień <see cref="SchedulePosition"/> dla podanego pokoju i ram czasowych.
        /// </summary>
        /// <param name="RoomId">ID pokoju</param>
        /// <param name="PeriodIndex">Indeks okienka czasowego w ciągu dnia</param>
        /// <param name="Day">Indeks dnia tygodnia</param>
        /// <param name="Weeks">Tygodnie, które należy wziąć pod uwagę</param>
        /// <returns>Kolekcję wystąpień pozycji w planie dla podanych parametrów</returns>
        /// <response code="200">Zwrócono kolekcję wystąpień</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        [Authorize]
        [HttpGet("Service.GetSchedulePositions({RoomId},{PeriodIndex},{Day},{Weeks})")]
        [CustomEnableQuery]
        [ODataRoute("Service.GetSchedulePositions(RoomId={RoomId},PeriodIndex={PeriodIndex},Day={Day},Weeks={Weeks})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetSchedulePositions([FromODataUri] int RoomId, [FromODataUri] int PeriodIndex, [FromODataUri] int Day, [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _schedulePositions = _unitOfWork.SchedulePositions
                    .Get(e => e.RoomId == RoomId && e.Timestamp.PeriodIndex == PeriodIndex
                                                 && e.Timestamp.Day == Day &&
                                                 Weeks.Contains(e.Timestamp.Week))
                    .Include(e => e.Timestamp);

                return Ok(_schedulePositions);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca liczby wystąpień <see cref="SchedulePosition"/> dla edycji zajęć w postaci listy obiektów klasy <see cref="ScheduleAmount"/>.
        /// </summary>
        /// <param name="CourseEditionIds">ID edycji zajęć</param>
        /// <returns>Liczby wystąpień <see cref="SchedulePosition"/> w postaci listy obiektów klasy <see cref="ScheduleAmount"/></returns>
        /// <response code="200">Zwrócono liczby wystąpień</response>
        /// <response code="400">
        /// Podano nieprawidłowe dane w parametrach;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize]
        [HttpGet("Service.GetScheduleAmount({CourseEditionIds})")]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetScheduleAmount([FromODataUri] IEnumerable<int> CourseEditionIds)
        {
            try
            {
                if (CourseEditionIds.Count() == 0)
                {
                    return BadRequest();
                }

                var _schedulePositions = _unitOfWork.SchedulePositions
                    .Get(e => CourseEditionIds.Contains(e.CourseEditionId))
                    .GroupBy(e => e.CourseEditionId)
                    .Select(e => new ScheduleAmount { CourseEditionId = e.Key, Count = e.Count() })
                    .ToList();

                return Ok(_schedulePositions);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca wystąpienia <see cref="SchedulePosition"/> spełniające kryteria podane w parametrach.
        /// </summary>
        /// <param name="CoordinatorsIds">ID prowadzących dla których zwrócone mają być pozycje w planie</param>
        /// <param name="GroupsIds">ID grup dla których zwrócone mają być pozycje w planie</param>
        /// <param name="RoomsIds">ID pokojów dla których zwrócone mają być pozycje w planie</param>
        /// <param name="Weeks">Tygodnie, które mają zostać wzięte pod uwagę</param>
        /// <returns>Listę wystąpień spełniających kryteria</returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        [Authorize]
        [HttpGet("Service.GetFilteredSchedule({CoordinatorsIds},{GroupsIds},{RoomsIds},{Weeks})")]
        [CustomEnableQuery(MaxExpansionDepth = 3)]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetFilteredSchedule(
            [FromODataUri] IEnumerable<int> CoordinatorsIds, 
            [FromODataUri] IEnumerable<int> GroupsIds, 
            [FromODataUri] IEnumerable<int> RoomsIds, 
            [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var coordinatorsCount = CoordinatorsIds.Count();
                var groupsCount = GroupsIds.Count();

                var coordinatorCourseEditionIds = new List<int>();
                if (coordinatorsCount > 0)
                {
                    coordinatorCourseEditionIds = _unitOfWork.CoordinatorCourseEditions
                        .GetAll()
                        .Where(e => CoordinatorsIds.Contains(e.CoordinatorId))
                        .Select(e => e.CourseEditionId).ToList();
                }

                var groupCourseEditionIds = new List<int>();
                if (groupsCount > 0)
                {
                    groupCourseEditionIds = _unitOfWork.GroupCourseEditions
                        .GetAll()
                        .Where(e => GroupsIds.Contains(e.GroupId))
                        .Select(e => e.CourseEditionId)
                        .ToList();
                }
                var courseEditionIds = coordinatorCourseEditionIds.Union(groupCourseEditionIds);

                var predicate = PredicateBuilder.New<SchedulePosition>(false);
                predicate = predicate
                    .Or(e => courseEditionIds.Contains(e.CourseEditionId));

                if (RoomsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => RoomsIds.Contains(e.RoomId));
                }

                var finalPredicate = predicate.And(e => Weeks.Contains(e.Timestamp.Week));


                var _schedulePositions = _unitOfWork.SchedulePositions
                    .Get(finalPredicate)
                    .Include(e => e.Timestamp);

                return Ok(_schedulePositions);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca informacje na temat dostępności pokojów w określonych ramach czasowych w postaci kolekcji obiektów klasy <see cref="RoomAvailability"/>.
        /// </summary>
        /// <param name="RoomsIds">ID pokojów</param>
        /// <param name="PeriodIndex">Indeks okienka czasowego w ciągu dnia</param>
        /// <param name="Day">Indeks dnia tygodnia</param>
        /// <param name="Weeks">Tygodnie, które należy wziąć pod uwagę</param>
        /// <returns>Kolekcję obiektów klasy <see cref="RoomAvailability"/> informujących o dostępności pokojów</returns>
        /// <response code="200">Zwrócono kolekcję oczekiwanych obiektów</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        [Authorize]
        [HttpGet("Service.GetRoomsAvailability({RoomsIds},{PeriodIndex},{Day},{Weeks})")]
        [CustomEnableQuery]
        [ODataRoute("Service.GetRoomsAvailability(RoomsIds={RoomsIds},PeriodIndex={PeriodIndex},Day={Day},Weeks={Weeks})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetRoomsAvailibility(
            [FromODataUri] IEnumerable<int> RoomsIds, 
            [FromODataUri] int PeriodIndex, 
            [FromODataUri] int Day, 
            [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _rooms = _unitOfWork.Rooms
                    .Get(e => RoomsIds.Contains(e.RoomId))
                    .Select(e => e.RoomId);

                var rooms = new Dictionary<int, RoomAvailability>();
                foreach (var _room in _rooms)
                {
                    rooms.TryAdd(_room, new RoomAvailability {RoomId = _room, IsBusy = false});
                }

                var _schedulePositions = _unitOfWork.SchedulePositions
                    .Get(e => _rooms.Contains(e.RoomId) && e.Timestamp.PeriodIndex == PeriodIndex 
                        && e.Timestamp.Day == Day && Weeks.Contains(e.Timestamp.Week))
                    .Include(e => e.Timestamp)
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
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Usuwa wszystkie wystąpienia <see cref="SchedulePosition"/> i powiązane z nimi dane
        /// - wystąpienia <see cref="Message"/>, <see cref="ScheduledMovePosition"/> i <see cref="ScheduledMove"/>.
        /// </summary>
        /// <returns>Informację o tym ile rekordów w bazie zostało usuniętych</returns>
        /// <response code="200">Usunięcie powiodło się</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.ClearSchedule")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ClearSchedule()
        {
            try
            {
                int messagesAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [Messages]");
                int scheduledMovePositionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [ScheduledMovePositions]");
                int scheduledMovesAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [ScheduledMoves]");
                int schedulePositionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [SchedulePositions]");

                return Ok(new 
                {
                    MessagesAffected = messagesAffected,
                    ScheduledMovePositionsAffected = scheduledMovePositionsAffected,
                    ScheduledMovesAffected = scheduledMovesAffected,
                    SchedulePositionsAffected = schedulePositionsAffected
                });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
