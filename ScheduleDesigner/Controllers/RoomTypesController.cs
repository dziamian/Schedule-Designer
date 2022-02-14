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
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="RoomType"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("RoomTypes")]
    public class RoomTypesController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public RoomTypesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tworzy nowe wystąpienie <see cref="RoomType"/>.
        /// </summary>
        /// <param name="roomTypeDto">Obiekt transferu danych</param>
        /// <returns>Nowo utworzone wystąpienie <see cref="RoomType"/></returns>
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

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="RoomType"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="RoomType"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetRoomTypes()
        {
            return Ok(_unitOfWork.RoomTypes.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="RoomType"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID typu pokoju</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="RoomType"/></returns>
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

        /// <summary>
        /// Nadpisuje pojedyncze wystąpienie <see cref="RoomType"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID typu pokoju</param>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanego wystąpienia</param>
        /// <returns>Nadpisane zażądane wystąpienie <see cref="RoomType"/></returns>
        /// <response code="200">Nadpisane zażądane wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie typu pokoju;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
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

        /// <summary>
        /// Usuwa pojedyncze wystąpienie <see cref="RoomType"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID typu pokoju</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z typem pokoju ze względu na wystąpienie z nim powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteRoomType([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.RoomTypes.Delete(e => e.RoomTypeId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                var roomIds = _unitOfWork.Rooms
                    .Get(e => e.RoomTypeId == key)
                    .Select(e => e.RoomId)
                    .ToList();

                var schedulePosition = await _unitOfWork.SchedulePositions
                    .Get(e => roomIds.Contains(e.RoomId)).FirstOrDefaultAsync();

                if (schedulePosition != null)
                {
                    return BadRequest("You cannot remove this room type because it contains some positions in schedule.");
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
        /// Usuwa wszystkie wystąpienia <see cref="RoomType"/>.
        /// </summary>
        /// <returns>Informację o tym ile rekordów w bazie zostało usuniętych</returns>
        /// <response code="200">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z typami pokojów ze względu na wystąpienie z nimi powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.ClearRoomTypes")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ClearRoomTypes()
        {
            try
            {
                var schedulePositions = _unitOfWork.SchedulePositions.GetAll();
                if (schedulePositions.Any())
                {
                    return BadRequest("You cannot clear room types because there are some positions in schedule assigned to them.");
                }

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
