using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
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
    /// Kontroler API przeznaczony do zarządzania <see cref="Settings"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("Settings")]
    public class SettingsController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public SettingsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Funkcja sprawdzająca poprawność danych w ustawieniach aplikacji (czy istnieje pełna liczba ram czasowych w ciągu pojedynczego dnia).
        /// </summary>
        /// <param name="settings">Instancja ustawień aplikacji</param>
        /// <returns>Prawdę jeśli dane są poprawne, w przeciwnym wypadku fałsz</returns>
        private static bool ArePeriodsValid(Settings settings)
        {
            return (settings.EndTime - settings.StartTime).TotalMinutes % settings.CourseDurationMinutes == 0;
        }

        /// <summary>
        /// Funkcja zwracająca liczbę ram czasowych w ciągu dnia na podstawie ustawień aplikacji.
        /// </summary>
        /// <param name="settings">Instancja ustawień aplikacji</param>
        /// <returns>Liczbę ram czasowych w ciągu pojedynczego dnia</returns>
        private static int GetNumberOfPeriods(Settings settings)
        {
            return (int)(settings.EndTime - settings.StartTime).TotalMinutes / settings.CourseDurationMinutes;
        }

        /// <summary>
        /// Zwraca ustawienia aplikacji (<see cref="Settings"/>).
        /// </summary>
        /// <returns>Ustawienia aplikacji w postaci obiektu klasy <see cref="Settings"/></returns>
        /// <response code="200">Zwrócono ustawienia</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono ustawień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetSettings()
        {
            try
            {
                var _settings = await _unitOfWork.Settings.GetFirst(e => true);
                if (_settings == null)
                {
                    return NotFound();
                }

                return Ok(_settings);
            } 
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca tablicę z etykietami wszystkich ram czasowych występujących w ciągu dnia.
        /// </summary>
        /// <returns>Tablicę z etykietami ram czasowych w ciągu dnia</returns>
        /// <response code="200">Zwrócono etykiety ram czasowych</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono ustawień</response>
        [Authorize]
        [HttpGet("Service.GetPeriods()")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPeriods()
        {
            try
            {
                var _settings = await _unitOfWork.Settings.GetFirst(e => true);
                if (_settings == null)
                {
                    return NotFound();
                }

                var numberOfPeriods = GetNumberOfPeriods(_settings) + 1;
                var currentPeriod = _settings.StartTime;
                var periodsStrings = new string[numberOfPeriods];
                var courseDuration = new TimeSpan(0, _settings.CourseDurationMinutes, 0);

                periodsStrings[0] = currentPeriod.ToString(@"hh\:mm");
                for (int i = 1; i < numberOfPeriods; ++i)
                {
                    currentPeriod = currentPeriod.Add(courseDuration);
                    periodsStrings[i] = currentPeriod.ToString(@"hh\:mm");
                }

                return Ok(periodsStrings);
            }
            catch (Exception e)
            {
                return BadRequest(e);
            }
        }

        /// <summary>
        /// Nadpisuje ustawienia aplikacji (<see cref="Settings"/>).
        /// </summary>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanych ustawień</param>
        /// <param name="connectionId">ID połączenia z centrum SignalR</param>
        /// <returns>Nadpisane ustawienia aplikacji</returns>
        /// <response code="200">Nadpisane ustawienia</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie ustawień;
        /// plan zajęć jest aktualnie zablokowany;
        /// nie zostało podane ID połączenia;
        /// tabela z przedmiotami nie jest pusta;
        /// plan zajęć nie jest pusty;
        /// nie zostały zablokowane wymagane edycje zajęć w bazie (wszystkie istniejące);
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono ustawień</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult UpdateSettings([FromBody] Delta<Settings> delta, [FromQuery] string connectionId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (connectionId == null)
            {
                return BadRequest("Could not find connection id.");
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
                        return NotFound();
                    }

                    delta.Patch(_settings);

                    if (!ArePeriodsValid(_settings))
                    {
                        ModelState.AddModelError("CoursesAmount", "Couldn't calculate the valid amount of max courses per day.");
                        return BadRequest(ModelState);
                    }

                    if (delta.GetChangedPropertyNames().Contains("CourseDurationMinutes"))
                    {
                        var courses = _unitOfWork.Courses.GetAll().FirstOrDefault();

                        if (courses != null)
                        {
                            return BadRequest("Courses must be empty in order to change their durations.");
                        }
                    }

                    var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                    var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

                    var courseEditions = _unitOfWork.CourseEditions.GetAll()
                        .ToList();

                    var courseEditionKeys = courseEditions.Select(e => new CourseEditionKey
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
                        var schedulePositions = _unitOfWork.SchedulePositions.GetAll().FirstOrDefault();

                        if (schedulePositions != null)
                        {
                            return BadRequest("Schedule must be empty in order to change settings.");
                        }

                        var notLockedCourseEditions = _unitOfWork.CourseEditions
                            .Get(e => e.LockUserId != userId || e.LockUserConnectionId != connectionId);

                        if (notLockedCourseEditions.Any())
                        {
                            return BadRequest("You did not lock all course editions.");
                        }

                        Methods.RemoveTimestamps(_unitOfWork);
                        Methods.AddTimestamps(_settings, _unitOfWork.Context.Database.GetConnectionString());

                        _unitOfWork.Complete();

                        return Ok(_settings);
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
    }
}
