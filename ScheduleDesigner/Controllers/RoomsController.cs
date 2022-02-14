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
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="Room"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("Rooms")]
    public class RoomsController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public RoomsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tworzy nowe wystąpienie <see cref="Room"/>.
        /// </summary>
        /// <param name="roomDto">Obiekt transferu danych</param>
        /// <returns>Nowo utworzone wystąpienie <see cref="Room"/></returns>
        /// <response code="201">Zwrócono nowo utworzone wystąpienie</response>
        /// <response code="400">
        /// Błędne dane w obiekcie transferu; 
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie udało się dodać nowo utworzonego wystąpienia do bazy danych</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
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

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="Room"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="Room"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetRooms()
        {
            return Ok(_unitOfWork.Rooms.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="Room"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID pokoju</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="Room"/></returns>
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

        /// <summary>
        /// Zwraca nazwy pokojów w postaci listy obiektów klasy <see cref="RoomName"/> dla podanych indentyfikatorów.
        /// </summary>
        /// <param name="RoomsIds">Kolekcja identyfikatorów pokojów</param>
        /// <returns>Listę nazw pokojów</returns>
        /// <response code="200">Zwrócono listę nazw pokojów</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono jednego z żądanych wystąpień</response>
        [Authorize]
        [HttpGet("Service.GetRoomsNames({RoomsIds})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
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

        /// <summary>
        /// Nadpisuje pojedyncze wystąpienie <see cref="Room"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID pokoju</param>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanego wystąpienia</param>
        /// <returns>Nadpisane zażądane wystąpienie <see cref="Room"/></returns>
        /// <response code="200">Nadpisane zażądane wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie pokoju;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
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

        /// <summary>
        /// Usuwa pojedyncze wystąpienie <see cref="Room"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID pokoju</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z pokojem ze względu na wystąpienie z nim powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteRoom([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.Rooms.Delete(e => e.RoomId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                var schedulePosition = await _unitOfWork.SchedulePositions
                    .Get(e => e.RoomId == key).FirstOrDefaultAsync();

                if (schedulePosition != null)
                {
                    return BadRequest("You cannot remove this room because it contains some positions in schedule.");
                }

                await _unitOfWork.CompleteAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Usuwa wszystkie wystąpienia <see cref="Room"/>.
        /// </summary>
        /// <returns>Informację o tym ile rekordów w bazie zostało usuniętych</returns>
        /// <response code="200">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z pokojami ze względu na wystąpienie z nimi powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.ClearRooms")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ClearRooms()
        {
            try
            {
                var schedulePositions = _unitOfWork.SchedulePositions.GetAll();
                if (schedulePositions.Any())
                {
                    return BadRequest("You cannot clear rooms because there are some positions in schedule assigned to them.");
                }

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
