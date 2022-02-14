using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="CourseType"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("CourseTypes")]
    public class CourseTypesController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public CourseTypesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tworzy nowe wystąpienie <see cref="CourseType"/>.
        /// </summary>
        /// <param name="courseTypeDto">Obiekt transferu danych</param>
        /// <returns>Nowo utworzone wystąpienie <see cref="CourseType"/></returns>
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
        public async Task<IActionResult> CreateCourseType([FromBody] CourseTypeDto courseTypeDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseType = await _unitOfWork.CourseTypes.Add(courseTypeDto.FromDto());

                if (_courseType != null)
                {
                    await _unitOfWork.CompleteAsync();
                    return Created(_courseType);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="CourseType"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="CourseType"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetCourseTypes()
        {
            return Ok(_unitOfWork.CourseTypes.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="CourseType"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID typu przedmiotu</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="CourseType"/></returns>
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
        public IActionResult GetCourseType([FromODataUri] int key)
        {
            try
            {
                var _courseType = _unitOfWork.CourseTypes.Get(e => e.CourseTypeId == key);
                if (!_courseType.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_courseType));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Nadpisuje pojedyncze wystąpienie <see cref="CourseType"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID typu przedmiotu</param>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanego wystąpienia</param>
        /// <returns>Nadpisane zażądane wystąpienie <see cref="CourseType"/></returns>
        /// <response code="200">Nadpisane zażądane wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie typu przedmiotu;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateCourseType([FromODataUri] int key, [FromBody] Delta<CourseType> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseType = await _unitOfWork.CourseTypes.GetFirst(e => e.CourseTypeId == key);
                if (_courseType == null)
                {
                    return NotFound();
                }

                delta.Patch(_courseType);

                await _unitOfWork.CompleteAsync();

                return Ok(_courseType);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Usuwa pojedyncze wystąpienie <see cref="CourseType"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID typu przedmiotu</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z typem przedmiotu ze względu na wystąpienie z nim powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteCourseType([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.CourseTypes.Delete(e => e.CourseTypeId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                var courseIds = _unitOfWork.Courses
                    .Get(e => e.CourseTypeId == key)
                    .Select(e => e.CourseId)
                    .ToList();

                var schedulePosition = await _unitOfWork.SchedulePositions
                    .Get(e => courseIds.Contains(e.CourseId)).FirstOrDefaultAsync();

                if (schedulePosition != null)
                {
                    return BadRequest("You cannot remove this course type because it contains some positions in schedule.");
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
        /// Usuwa wszystkie wystąpienia <see cref="CourseType"/>.
        /// </summary>
        /// <returns>Informację o tym ile rekordów w bazie zostało usuniętych</returns>
        /// <response code="200">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z typami przedmiotów ze względu na wystąpienie z nimi powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.ClearCourseTypes")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ClearCourseTypes()
        {
            try
            {
                var schedulePositions = _unitOfWork.SchedulePositions.GetAll();
                if (schedulePositions.Any())
                {
                    return BadRequest("You cannot clear course types because there are some positions in schedule assigned to them.");
                }

                int courseTypesAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [CourseTypes]");

                return Ok(new { CourseTypesAffected = courseTypesAffected });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
