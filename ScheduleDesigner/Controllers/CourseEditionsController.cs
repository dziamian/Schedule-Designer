using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqKit;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Hubs.Interfaces;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Dtos;
using System.Threading;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="CourseEdition"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("CourseEditions")]
    public class CourseEditionsController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public CourseEditionsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tworzy nowe wystąpienie <see cref="CourseEdition"/>.
        /// </summary>
        /// <param name="courseEditionDto">Obiekt transferu danych</param>
        /// <returns>Nowo utworzone wystąpienie <see cref="CourseEdition"/></returns>
        /// <response code="201">Zwrócono nowo utworzone wystąpienie</response>
        /// <response code="400">
        /// Błędne dane w obiekcie transferu; 
        /// plan zajęć jest aktualnie zablokowany;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie udało się dodać nowo utworzonego wystąpienia do bazy danych</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult CreateCourseEdition([FromBody] CourseEditionDto courseEditionDto)
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
                    var _courseEdition = _unitOfWork.CourseEditions.Add(courseEditionDto.FromDto()).Result;

                    if (_courseEdition != null)
                    {
                        _unitOfWork.Complete();
                        return Created(_courseEdition);
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
        /// Zwraca wystąpienia <see cref="CourseEdition"/> spełniające kryteria podane w parametrach.
        /// </summary>
        /// <param name="CoordinatorsIds">ID prowadzących dla których zwrócone mają być edycje zajęć</param>
        /// <param name="GroupsIds">ID grup dla których zwrócone mają być edycje zajęć</param>
        /// <param name="RoomsIds">ID pokojów dla których zwrócone mają być edycje zajęć</param>
        /// <param name="Frequency">Maksymalna liczba jednostek zajęciowych możliwych do ustawienia na planie (możliwa częstotliwość)</param>
        /// <returns>Listę wystąpień spełniających kryteria</returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        /// <response code="400">
        /// Ustawienia aplikacji nie zostały odnalezione;
        /// nieprawidłowa częstotliwość;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize]
        [CustomEnableQuery(MaxExpansionDepth = 3)]
        [HttpGet("Service.GetFilteredCourseEditions({CoordinatorsIds},{GroupsIds},{RoomsIds},{Frequency})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetFilteredCourseEditions(
            [FromODataUri] IEnumerable<int> CoordinatorsIds,
            [FromODataUri] IEnumerable<int> GroupsIds,
            [FromODataUri] IEnumerable<int> RoomsIds,
            [FromODataUri] int Frequency)
        {
            var _settings = await _unitOfWork.Settings.GetFirst(e => true);
            if (_settings == null)
            {
                return BadRequest("Application settings has not been specified.");
            }

            if (Frequency > _settings.TermDurationWeeks || Frequency <= 0)
            {
                return BadRequest("Frequency is invalid");
            }

            try
            {
                var predicate = PredicateBuilder.New<CourseEdition>();
                if (CoordinatorsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Coordinators.Any(f => CoordinatorsIds.Contains(f.CoordinatorId)));
                }
                if (GroupsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Groups.Any(f => GroupsIds.Contains(f.GroupId)));
                }
                if (RoomsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Course.Rooms.Any(f => RoomsIds.Contains(f.RoomId)));
                }

                var courseDurationMinutes = _settings.CourseDurationMinutes;
                var totalMinutes = Frequency * courseDurationMinutes;

                var finalPredicate = predicate
                    .And(e => Math.Ceiling(e.Course.UnitsMinutes / (courseDurationMinutes * 1.0) - e.SchedulePositions.Count) >= Frequency);
                
                var _courseEditions = _unitOfWork.CourseEditions
                    .Get(finalPredicate) 
                    .Include(e => e.SchedulePositions)
                    .Include(e => e.Course)
                    .Include(e => e.Coordinators)
                    .Include(e => e.Groups)
                    .ToList();
                
                return Ok(_courseEditions);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="CourseEdition"/> spełniające kryteria podane w parametrach.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID edycji zajęć</param>
        /// <param name="CoordinatorsIds">ID prowadzących dla których zwrócona ma być edycja zajęć</param>
        /// <param name="GroupsIds">ID grup dla których zwrócona ma być edycja zajęć</param>
        /// <param name="RoomsIds">ID pokojów dla których zwrócona ma być edycja zajęć</param>
        /// <param name="Frequency">Maksymalna liczba jednostek zajęciowych możliwych do ustawienia na planie (możliwa częstotliwość)</param>
        /// <returns>Wystąpienie spełniające kryteria</returns>
        /// <response code="200">Zwrócono odnalezione wystąpienie</response>
        /// <response code="400">
        /// Ustawienia aplikacji nie zostały odnalezione;
        /// nieprawidłowa częstotliwość;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize]
        [CustomEnableQuery(MaxExpansionDepth = 3)]
        [HttpGet("{key1},{key2}/Service.GetFilteredCourseEdition({CoordinatorsIds},{GroupsIds},{RoomsIds},{Frequency})")]
        [ODataRoute("({key1},{key2})/GetFilteredCourseEdition(CoordinatorsIds={CoordinatorsIds},GroupsIds={GroupsIds},RoomsIds={RoomsIds},Frequency={Frequency})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> GetFilteredCourseEdition(
            [FromODataUri] int key1, [FromODataUri] int key2,
            [FromODataUri] IEnumerable<int> CoordinatorsIds,
            [FromODataUri] IEnumerable<int> GroupsIds,
            [FromODataUri] IEnumerable<int> RoomsIds,
            [FromODataUri] double Frequency)
        {
            var _settings = await _unitOfWork.Settings.GetFirst(e => true);
            if (_settings == null)
            {
                return BadRequest("Application settings has not been specified.");
            }

            if (Frequency > _settings.TermDurationWeeks || Frequency <= 0)
            {
                return BadRequest("Frequency is invalid");
            }

            try
            {
                var predicate = PredicateBuilder.New<CourseEdition>();
                if (CoordinatorsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Coordinators.Any(f => CoordinatorsIds.Contains(f.CoordinatorId)));
                }
                if (GroupsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Groups.Any(f => GroupsIds.Contains(f.GroupId)));
                }
                if (RoomsIds.Count() > 0)
                {
                    predicate = predicate
                        .Or(e => e.Course.Rooms.Any(f => RoomsIds.Contains(f.RoomId)));
                }
                var courseDurationMinutes = _settings.CourseDurationMinutes;
                var totalMinutes = Frequency * courseDurationMinutes;

                var finalPredicate = PredicateBuilder.New<CourseEdition>()
                    .And(e => e.CourseId == key1 && e.CourseEditionId == key2)
                    .And(predicate)
                    .And(e => Math.Ceiling(e.Course.UnitsMinutes / (courseDurationMinutes * 1.0) - e.SchedulePositions.Count) >= Frequency);

                var _courseEditions = _unitOfWork.CourseEditions
                    .Get(finalPredicate)
                    .Include(e => e.SchedulePositions)
                    .Include(e => e.Course)
                        .ThenInclude(e => e.Rooms)
                    .Include(e => e.Coordinators);

                return Ok(SingleResult.Create(_courseEditions));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca kolekcję zajętych ram czasowych w planie powodujących konflikty dla konkretnej edycji zajęć.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID edycji zajęć</param>
        /// <param name="Weeks">Tygodnie, dla których należy znaleźć zajęte ramy czasowe</param>
        /// <returns>Kolekcję zajętych ram czasowych dla edycji zajęć</returns>
        /// <response code="200">Zwrócono kolekcję zajętych ram czasowych</response>
        /// <response code="400">
        /// Ustawienia aplikacji nie zostały odnalezione;
        /// podano nieprawidłowe tygodnie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanej edycji zajęć w bazie danych</response>
        [Authorize]
        [HttpGet("{key1},{key2}/Service.GetBusyPeriods({Weeks})")]
        [CustomEnableQuery]
        [ODataRoute("({key1},{key2})/GetBusyPeriods(Weeks={Weeks})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetBusyPeriods([FromODataUri] int key1, [FromODataUri] int key2, [FromODataUri] IEnumerable<int> Weeks)
        {
            var _settings = await _unitOfWork.Settings.GetFirst(e => true);
            if (_settings == null)
            {
                return BadRequest("Application settings has not been specified.");
            }

            if (Weeks.Any(week => week > _settings.TermDurationWeeks || week <= 0))
            {
                return BadRequest("Weeks are invalid");
            }

            try
            {
                var _courseEdition = _unitOfWork.CourseEditions
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2)
                    .Include(e => e.Coordinators)
                    .Include(e => e.Groups)
                        .ThenInclude(e => e.Group);

                if (!_courseEdition.Any())
                {
                    return NotFound();
                }

                var courseEdition = await _courseEdition.FirstOrDefaultAsync();

                var coordinatorsIds = courseEdition.Coordinators.Select(e => e.CoordinatorId).ToList();

                var groupsIds = Methods.GetNestedGroupsIds(courseEdition, _unitOfWork.Groups);

                var _timestamps = _unitOfWork.SchedulePositions
                    .Get(e => Weeks.Contains(e.Timestamp.Week) 
                              && (e.CourseEdition.Coordinators.Select(e => e.CoordinatorId).Any(e => coordinatorsIds.Contains(e))
                              || e.CourseEdition.Groups.Select(e => e.GroupId).Any(e => groupsIds.Contains(e))))
                    .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Coordinators)
                    .Include(e => e.CourseEdition)
                        .ThenInclude(e => e.Groups)
                    .Include(e => e.Timestamp)
                    .Select(e => e.Timestamp);


                return Ok(_timestamps.Any() ? _timestamps : Enumerable.Empty<Timestamp>().AsQueryable());
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca informację czy dana rama czasowa jest zajęta dla konkretnej edycji zajęć.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID edycji zajęć</param>
        /// <param name="PeriodIndex">Indeks okienka czasowego w ciągu dnia</param>
        /// <param name="Day">Indeks dnia tygodnia</param>
        /// <param name="Weeks">Tygodnie, które należy wziąć pod uwagę</param>
        /// <returns>Prawdę jeśli ustawienie zajęć w podanej ramie czasowej spowoduje konflikty, w przeciwnym razie fałsz</returns>
        /// <response code="200">Zwrócono informację czy rama czasowa jest zajęta</response>
        /// <response code="400">
        /// Nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanej edycji zajęć w bazie danych</response>
        [Authorize]
        [HttpGet("{key1},{key2}/Service.IsPeriodBusy({PeriodIndex},{Day},{Weeks})")]
        [CustomEnableQuery]
        [ODataRoute("({key1},{key2})/IsPeriodBusy(PeriodIndex={PeriodIndex},Day={Day},Weeks={Weeks})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> IsPeriodBusy(
            [FromODataUri] int key1, 
            [FromODataUri] int key2, 
            [FromODataUri] int PeriodIndex, 
            [FromODataUri] int Day, 
            [FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var _courseEdition = _unitOfWork.CourseEditions
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2)
                    .Include(e => e.Coordinators)
                    .Include(e => e.Groups)
                    .ThenInclude(e => e.Group);

                if (!_courseEdition.Any())
                {
                    return NotFound();
                }

                var courseEdition = await _courseEdition.FirstOrDefaultAsync();

                var coordinatorsIds = courseEdition.Coordinators.Select(e => e.CoordinatorId).ToList();

                var groupsIds = Methods.GetNestedGroupsIds(courseEdition, _unitOfWork.Groups);

                var _timestamps = _unitOfWork.SchedulePositions
                    .Get(e => e.Timestamp.PeriodIndex == PeriodIndex
                              && e.Timestamp.Day == Day
                              && Weeks.Contains(e.Timestamp.Week)
                              && e.Timestamp.PeriodIndex == PeriodIndex &&
                              e.Timestamp.Day == Day
                              && (e.CourseEdition.Coordinators.Select(e => e.CoordinatorId)
                                      .Any(e => coordinatorsIds.Contains(e))
                                  || e.CourseEdition.Groups.Select(e => e.GroupId).Any(e => groupsIds.Contains(e))))
                    .Include(e => e.CourseEdition)
                    .ThenInclude(e => e.Coordinators)
                    .Include(e => e.CourseEdition)
                    .ThenInclude(e => e.Groups)
                    .Include(e => e.Timestamp);

                return Ok(_timestamps.Any());
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca łączny rozmiar grup studenckich dla danej edycji zajęć.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID edycji zajęć</param>
        /// <returns>Łączny rozmiar grup studenckich dla edycji zajęć</returns>
        /// <response code="200">Zwrócono rozmiar grup</response>
        /// <response code="400">
        /// Nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanej edycji zajęć lub przypisanej do niej grupy studenckiej w bazie danych</response>
        [Authorize]
        [HttpGet("{key1},{key2}/Service.GetCourseEditionGroupsSize()")]
        [ODataRoute("({key1},{key2})/GetCourseEditionGroupsSize()")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetCourseEditionGroupsSize([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _courseEdition = _unitOfWork.CourseEditions
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2)
                    .Include(e => e.Groups);

                if (!_courseEdition.Any())
                {
                    return NotFound();
                }
                var courseEdition = _courseEdition.FirstOrDefault();
                var courseEditionGroupsIds = courseEdition.Groups.Select(e => e.GroupId);

                var _groups = _unitOfWork.Groups
                    .Get(e => courseEditionGroupsIds.Contains(e.GroupId));

                if (!_groups.Any())
                {
                    return NotFound();
                }
                var groupsIds = _groups.Select(e => e.GroupId).ToList();
                var size = groupsIds.Count();


                var _childGroups = _unitOfWork.Groups
                    .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(0, size).Contains((int)e.ParentGroupId));

                var startIndex = 0;
                var endIndex = groupsIds.Count;

                while (_childGroups.Any())
                {
                    groupsIds.AddRange(_childGroups.Select(e => e.GroupId).ToList());

                    startIndex = endIndex;
                    endIndex += _childGroups.Count();

                    _childGroups = _unitOfWork.Groups
                        .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(startIndex, endIndex - startIndex).Contains((int)e.ParentGroupId));
                }

                var groupsSize = _unitOfWork.StudentGroups
                    .Get(e => groupsIds.Contains(e.GroupId))
                    .GroupBy(e => e.StudentId)
                    .Select(e => e.Key).Count();

                return Ok(groupsSize);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="CourseEdition"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="CourseEdition"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetCourseEditions()
        {
            return Ok(_unitOfWork.CourseEditions.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="CourseEdition"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID edycji zajęć</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="CourseEdition"/></returns>
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
        public IActionResult GetCourseEdition([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _courseEdition = _unitOfWork.CourseEditions
                    .Get(e => e.CourseId == key1 && e.CourseEditionId == key2);
                if (!_courseEdition.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_courseEdition));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Nadpisuje pojedyncze wystąpienie <see cref="CourseEdition"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID edycji zajęć</param>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanego wystąpienia</param>
        /// <returns>Nadpisane zażądane wystąpienie <see cref="CourseEdition"/></returns>
        /// <response code="200">Nadpisane zażądane wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie edycji zajęć;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch("{key1},{key2}")]
        [ODataRoute("({key1},{key2})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> UpdateCourseEdition([FromODataUri] int key1, [FromODataUri] int key2, [FromBody] Delta<CourseEdition> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseEdition = await _unitOfWork.CourseEditions
                    .GetFirst(e => e.CourseId == key1 && e.CourseEditionId == key2);
                if (_courseEdition == null)
                {
                    return NotFound();
                }

                delta.Patch(_courseEdition);

                await _unitOfWork.CompleteAsync();

                return Ok(_courseEdition);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Usuwa pojedyncze wystąpienie <see cref="CourseEdition"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key1">ID przedmiotu</param>
        /// <param name="key2">ID edycji zajęć</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z edycją zajęć ze względu na wystąpienie z nią powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key1},{key2}")]
        [ODataRoute("({key1},{key2})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteCourseEdition([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var result = await _unitOfWork.CourseEditions
                    .Delete(e => e.CourseId == key1 && e.CourseEditionId == key2);
                if (result < 0)
                {
                    return NotFound();
                }

                var schedulePosition = await _unitOfWork.SchedulePositions
                    .Get(e => e.CourseEditionId == key2).FirstOrDefaultAsync();

                if (schedulePosition != null)
                {
                    return BadRequest("You cannot remove this course edition because it contains some positions in schedule.");
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
        /// Usuwa wszystkie wystąpienia <see cref="CourseEdition"/> i powiązane z nimi dane 
        /// - wystąpienia <see cref="GroupCourseEdition"/> i <see cref="CoordinatorCourseEdition"/>.
        /// </summary>
        /// <returns>Informację o tym ile rekordów w bazie zostało usuniętych</returns>
        /// <response code="200">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z edycjami zajęć ze względu na wystąpienie z nimi powiązań w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.ClearCourseEditions")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ClearCourseEditions()
        {
            try
            {
                var schedulePositions = _unitOfWork.SchedulePositions.GetAll();
                if (schedulePositions.Any())
                {
                    return BadRequest("You cannot clear course editions because there are some positions in schedule assigned to them.");
                }

                int groupCourseEditionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [GroupCourseEditions]");
                int coordinatorCourseEditionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [CoordinatorCourseEditions]");
                int courseEditionsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [CourseEditions]");

                return Ok(new
                { 
                    GroupCourseEditionsAffected = groupCourseEditionsAffected,
                    CoordinatorCourseEditionsAffected = coordinatorCourseEditionsAffected,
                    CourseEditionsAffected = courseEditionsAffected
                });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
