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
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using Microsoft.EntityFrameworkCore;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="CourseRoom"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("CourseRooms")]
    public class CourseRoomsController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public CourseRoomsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tworzy nowe wystąpienie <see cref="CourseRoom"/>.
        /// </summary>
        /// <param name="courseRoomDto">Obiekt transferu danych</param>
        /// <returns>Nowo utworzone wystąpienie <see cref="CourseRoom"/></returns>
        /// <response code="201">Zwrócono nowo utworzone wystąpienie</response>
        /// <response code="400">
        /// Błędne dane w obiekcie transferu; 
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie udało się dodać nowo utworzonego wystąpienia do bazy danych</response>
        [Authorize(Policy = "Assistant")]
        [HttpPost]
        [ODataRoute("")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CreateCourseRoom([FromBody] CourseRoomDto courseRoomDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);
                var isAdmin = HttpContext.User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Administrator");

                if (!isAdmin)
                {
                    courseRoomDto.UserId = userId;
                } 
                else
                {
                    courseRoomDto.UserId = null;
                }

                var _courseRoom = await _unitOfWork.CourseRooms.Add(courseRoomDto.FromDto());

                if (_courseRoom == null)
                {
                    return NotFound();
                }
                
                await _unitOfWork.CompleteAsync();
                return Created(_courseRoom);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="CourseRoom"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="CourseRoom"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetCourseRooms()
        {
            return Ok(_unitOfWork.CourseRooms.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="CourseRoom"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID pokoju</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="CourseRoom"/></returns>
        /// <response code="200">Zwrócono żądane wystąpienie</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize]
        [HttpGet("{key1},{key2}")]
        [CustomEnableQuery]
        [ODataRoute("({key1},{key2})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetCourseRoom([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _courseRoom = _unitOfWork.CourseRooms
                    .Get(e => e.CourseId == key1 && e.RoomId == key2);
                if (!_courseRoom.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_courseRoom));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Nadpisuje pojedyncze wystąpienie <see cref="CourseRoom"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID pokoju</param>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanego wystąpienia</param>
        /// <returns>Nadpisane zażądane wystąpienie <see cref="CourseRoom"/></returns>
        /// <response code="200">Nadpisane zażądane wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie przypisania pokoju do przedmiotu;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch("{key1},{key2}")]
        [ODataRoute("({key1},{key2})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateCourseRoom([FromODataUri] int key1, [FromODataUri] int key2, [FromBody] Delta<CourseRoom> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseRoom = await _unitOfWork.CourseRooms
                    .GetFirst(e => e.CourseId == key1 && e.RoomId == key2);
                if (_courseRoom == null)
                {
                    return NotFound();
                }

                delta.Patch(_courseRoom);

                await _unitOfWork.CompleteAsync();

                return Ok(_courseRoom);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Usuwa pojedyncze wystąpienie <see cref="CourseRoom"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID pokoju</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z przypisaniem pokoju do przedmiotu ze względu na wystąpienie z nim powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key1},{key2}")]
        [ODataRoute("({key1},{key2})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteCourseRoom([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var result = await _unitOfWork.CourseRooms
                    .Delete(e => e.CourseId == key1 && e.RoomId == key2);
                if (result < 0)
                {
                    return NotFound();
                }

                var schedulePosition = await _unitOfWork.SchedulePositions
                    .Get(e => e.RoomId == key2).FirstOrDefaultAsync();

                if (schedulePosition != null)
                {
                    return BadRequest("You cannot remove this room from course because it contains some positions in schedule.");
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
