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
using ScheduleDesigner.Repositories.Interfaces;
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
    /// Kontroler API przeznaczony do zarządzania <see cref="Course"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("Courses")]
    public class CoursesController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public CoursesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tworzy nowe wystąpienie <see cref="Course"/>.
        /// </summary>
        /// <param name="courseDto">Obiekt transferu danych</param>
        /// <returns>Nowo utworzone wystąpienie <see cref="Course"/></returns>
        /// <response code="201">Zwrócono nowo utworzone wystąpienie</response>
        /// <response code="400">
        /// Błędne dane w obiekcie transferu; 
        /// plan zajęć jest aktualnie zablokowany;
        /// nie odnaleziono ustawień aplikacji;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie udało się dodać nowo utworzonego wystąpienia do bazy danych</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult CreateCourse([FromBody] CourseDto courseDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var _settings = _unitOfWork.Settings.GetFirst(e => true).Result;
                    if (_settings == null)
                    {
                        return BadRequest("Application settings has not been specified.");
                    }

                    if (!Methods.AreUnitsMinutesValid(courseDto.UnitsMinutes, _unitOfWork.Settings.GetFirst(e => true).Result))
                    {
                        ModelState.AddModelError("CourseUnitsMinutes", "Could not calculate the valid amount of courses in term.");
                        return BadRequest(ModelState);
                    }

                    var _course = _unitOfWork.Courses.Add(courseDto.FromDto()).Result;

                    if (_course != null)
                    {
                        _unitOfWork.Complete();
                        return Created(_course);
                    }
                    return NotFound();
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
        /// Zwraca wszystkie wystąpienia <see cref="Course"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="Course"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetCourses()
        {
            return Ok(_unitOfWork.Courses.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="Course"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID przedmiotu</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="Course"/></returns>
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
        public IActionResult GetCourse([FromODataUri] int key)
        {
            try
            {
                var _course = _unitOfWork.Courses.Get(e => e.CourseId == key);
                if (!_course.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_course));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Nadpisuje pojedyncze wystąpienie <see cref="Course"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID przedmiotu</param>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanego wystąpienia</param>
        /// <param name="connectionId">ID połączenia z centrum SignalR</param>
        /// <returns>Nadpisane zażądane wystąpienie <see cref="Course"/></returns>
        /// <response code="200">Nadpisane zażądane wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie przedmiotu;
        /// nie odnaleziono ustawień aplikacji;
        /// plan zajęć jest aktualnie zablokowany;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult UpdateCourse([FromODataUri] int key, [FromBody] Delta<Course> delta, [FromQuery] string connectionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var settings = _unitOfWork.Settings.GetFirst(e => true).Result;

            if (settings == null)
            {
                return BadRequest("Application settings are not specified.");
            }

            try
            {
                if (!Monitor.TryEnter(SchedulePositionsController.ScheduleLock, SchedulePositionsController.LockTimeout))
                {
                    return BadRequest("Schedule is locked right now. Please try again later.");
                }
                try
                {
                    var _course = _unitOfWork.Courses.GetFirst(e => e.CourseId == key).Result;
                    if (_course == null)
                    {
                        return NotFound();
                    }

                    if (!delta.GetChangedPropertyNames().Contains("UnitsMinutes"))
                    {
                        delta.Patch(_course);

                        _unitOfWork.Complete();

                        return Ok(_course);
                    }

                    var _settings = _unitOfWork.Settings.GetFirst(e => true).Result;
                    if (!Methods.AreUnitsMinutesValid(_course.UnitsMinutes, _settings))
                    {
                        ModelState.AddModelError("CourseUnitsMinutes", "Couldn't calculate the valid amount of courses in term.");
                        return BadRequest(ModelState);
                    }

                    var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);
                    if (connectionId == null)
                    {
                        return BadRequest("Connection id not found.");
                    }
                    
                    var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

                    var courseEditionKeys = _unitOfWork.CourseEditions
                        .Get(e => e.CourseId == key).Select(e => new CourseEditionKey
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
                        var notLockedCourseEditions = _unitOfWork.CourseEditions
                            .Get(e => e.CourseId == key
                                && (e.LockUserId != userId || e.LockUserConnectionId != connectionId));

                        if (notLockedCourseEditions.Any())
                        {
                            return BadRequest("You did not lock all editions for chosen course.");
                        }
                        
                        delta.TryGetPropertyValue("UnitsMinutes", out var unitsMinutesObject);
                        var unitsMinutes = (int)unitsMinutesObject;

                        if (_course.UnitsMinutes > unitsMinutes) 
                        {
                            var schedulePositionCounts = _unitOfWork.SchedulePositions
                                .Get(e => e.CourseId == key)
                                .GroupBy(e => e.CourseEditionId)
                                .Select(e => new { e.Key, Count = e.Count() })
                                .ToList();

                            var maxCourseUnits = (int)Math.Ceiling(unitsMinutes / (settings.CourseDurationMinutes * 1.0));
                            if (schedulePositionCounts.Any(e => e.Count > maxCourseUnits))
                            {
                                return BadRequest("There is already too many units of some course edition in the schedule.");
                            }
                        }

                        delta.Patch(_course);

                        _unitOfWork.Complete();

                        return Ok(_course);
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
        /// Usuwa pojedyncze wystąpienie <see cref="Course"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID przedmiotu</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z przedmiotem ze względu na wystąpienie z nim powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteCourse([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.Courses.Delete(e => e.CourseId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                var schedulePosition = await _unitOfWork.SchedulePositions
                    .Get(e => e.CourseId == key).FirstOrDefaultAsync();

                if (schedulePosition != null)
                {
                    return BadRequest("You cannot remove this course because it contains some positions in schedule.");
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
        /// Usuwa wszystkie wystąpienia <see cref="Course"/> i powiązane z nimi dane 
        /// - wystąpienia <see cref="CourseRoom"/>.
        /// </summary>
        /// <returns>Informację o tym ile rekordów w bazie zostało usuniętych</returns>
        /// <response code="200">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z przedmiotami ze względu na wystąpienie z nimi powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.ClearCourses")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ClearCourses()
        {
            try
            {
                var schedulePositions = _unitOfWork.SchedulePositions.GetAll();
                if (schedulePositions.Any())
                {
                    return BadRequest("You cannot clear courses because there are some positions in schedule assigned to them.");
                }

                int courseRoomsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [CourseRooms]");
                int coursesAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [Courses]");

                return Ok(new { CourseRoomsAffected = courseRoomsAffected, CoursesAffected = coursesAffected });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
