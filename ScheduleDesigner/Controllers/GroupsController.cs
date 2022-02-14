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
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Repositories.UnitOfWork;
using ScheduleDesigner.Attributes;
using System.Threading;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Hubs;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="Group"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("Groups")]
    public class GroupsController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public GroupsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tworzy nowe wystąpienie <see cref="Group"/>.
        /// </summary>
        /// <param name="groupDto">Obiekt transferu danych</param>
        /// <returns>Nowo utworzone wystąpienie <see cref="Group"/></returns>
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
        public IActionResult CreateGroup([FromBody] GroupDto groupDto)
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
                    if (groupDto.ParentGroupId != null)
                    {
                        var parentGroup = _unitOfWork.Groups
                            .Get(e => e.GroupId == groupDto.ParentGroupId)
                            .FirstOrDefault();

                        if (parentGroup == null) 
                        {
                            return BadRequest("Could not find parent group you have chosen.");
                        }
                    }

                    var _group = _unitOfWork.Groups.Add(groupDto.FromDto()).Result;

                    if (_group != null)
                    {
                        _unitOfWork.Complete();
                        return Created(_group);
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
        /// Zwraca wszystkie wystąpienia <see cref="Group"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="Group"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetGroups()
        {
            return Ok(_unitOfWork.Groups.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="Group"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID grupy</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="Group"/></returns>
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
        public IActionResult GetGroup([FromODataUri] int key)
        {
            try
            {
                var _group = _unitOfWork.Groups
                    .Get(e => e.GroupId == key);
                if (!_group.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_group));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca informację o pełnej nazwie grupy o podanym ID w postaci obiektu klasy <see cref="GroupFullName"/>.
        /// </summary>
        /// <param name="key">ID grupy</param>
        /// <returns>Obiekt klasy <see cref="GroupFullName"/> zawierający informacje o pełnej nazwie grupy</returns>
        /// <response code="200">Zwrócono obiekt z informacją o pełnej nazwie grupy</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono żądanej grupy</response>
        [Authorize]
        [HttpGet("{key}/Service.GetGroupFullName()")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGroupFullName([FromODataUri] int key)
        {
            try
            {
                var _group = _unitOfWork.Groups
                    .Get(e => e.GroupId == key)
                    .Include(e => e.ParentGroup);

                if (!_group.Any())
                {
                    return NotFound();
                }

                var group = await _group.FirstAsync();
                var basicGroupName = group.Name;
                var result = Methods.GetParentGroupsWithFullNameAndLevels(_group, group);

                return Ok(new GroupFullName { BasicName = basicGroupName, FullName = result.Item2, GroupsIds = result.Item1, Levels = result.Item3 });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca dużą liczbę informacji o grupie o podanym ID w postaci obiektu klasy <see cref="GroupFullInfo"/>.
        /// </summary>
        /// <param name="key">ID grupy</param>
        /// <returns>Obiekt klasy <see cref="GroupFullInfo"/> zawierający dużą liczbę informacji o grupie</returns>
        /// <response code="200">Zwrócono obiekt z dużą liczbą informacji o grupie</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono żądanej grupy</response>
        [Authorize]
        [HttpGet("{key}/Service.GetGroupFullInfo()")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGroupFullInfo([FromODataUri] int key)
        {
            try
            {
                var _group = _unitOfWork.Groups
                    .Get(e => e.GroupId == key)
                    .Include(e => e.ParentGroup);

                if (!_group.Any())
                {
                    return NotFound();
                }

                var group = await _group.FirstAsync();
                var basicGroupName = group.Name;
                var result1 = Methods.GetParentGroupsWithFullNameAndLevels(_group, group);
                var result2 = Methods.GetChildGroups(new List<Group> { group }, _unitOfWork.Groups);

                return Ok(new GroupFullInfo { GroupId = group.GroupId, BasicName = basicGroupName, FullName = result1.Item2, ParentIds = result1.Item1, ChildIds = result2 });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca informacje o pełnych nazwach grup o podanych ID w postaci listy obiektów klasy <see cref="GroupFullName"/>.
        /// </summary>
        /// <param name="GroupsIds">Kolekcja ID grup</param>
        /// <returns>Listę obiektów klasy <see cref="GroupFullName"/> zawierających informacje o pełnych nazwach grup</returns>
        /// <response code="200">Zwrócono listę obiektów z informacjami o pełnych nazwach grup</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono jednej z żądanych grup</response>
        [Authorize]
        [HttpGet("Service.GetGroupsFullNames({GroupsIds})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGroupsFullNames([FromODataUri] IEnumerable<int> GroupsIds)
        {
            try
            {
                var groupsFullNamesList = new List<GroupFullName>();
                var groupsFullNamesDictionary = new Dictionary<int, GroupFullName>();

                foreach (var _groupId in GroupsIds)
                {
                    if (groupsFullNamesDictionary.TryGetValue(_groupId, out var groupFullName))
                    {
                        groupsFullNamesList.Add(groupFullName);
                        continue;
                    }

                    var _group = _unitOfWork.Groups
                    .Get(e => e.GroupId == _groupId)
                    .Include(e => e.ParentGroup);

                    if (!_group.Any())
                    {
                        return NotFound();
                    }

                    var group = await _group.FirstAsync();
                    var basicGroupName = group.Name;
                    var result = Methods.GetParentGroupsWithFullNameAndLevels(_group, group);

                    groupFullName = new GroupFullName { BasicName = basicGroupName, FullName = result.Item2, GroupsIds = result.Item1, Levels = result.Item3 };
                    groupsFullNamesDictionary.Add(result.Item1[0], groupFullName);
                    groupsFullNamesList.Add(groupFullName);
                }

                return Ok(groupsFullNamesList);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca dużą liczbę informacji o grupach o podanych ID w postaci listy obiektów klasy <see cref="GroupFullInfo"/>.
        /// </summary>
        /// <param name="GroupsIds">Kolekcja ID grup</param>
        /// <returns>Listę obiektów klasy <see cref="GroupFullInfo"/> zawierających dużą liczbę informacji o grupach</returns>
        /// <response code="200">Zwrócono listę obiektów z dużą liczbą informacji o grupach</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono jednej z żądanych grup</response>
        [Authorize]
        [HttpGet("Service.GetGroupsFullInfo({GroupsIds})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetGroupsFullInfo([FromODataUri] IEnumerable<int> GroupsIds)
        {
            try
            {
                var groupsFullInfoList = new List<GroupFullInfo>();
                var groupsFullInfoDictionary = new Dictionary<int, GroupFullInfo>();

                foreach (var _groupId in GroupsIds)
                {
                    if (groupsFullInfoDictionary.TryGetValue(_groupId, out var groupFullInfo))
                    {
                        groupsFullInfoList.Add(groupFullInfo);
                        continue;
                    }

                    var _group = _unitOfWork.Groups
                    .Get(e => e.GroupId == _groupId)
                    .Include(e => e.ParentGroup);

                    if (!_group.Any())
                    {
                        return NotFound();
                    }

                    var group = await _group.FirstAsync();
                    var basicGroupName = group.Name;
                    var result1 = Methods.GetParentGroupsWithFullNameAndLevels(_group, group);
                    var result2 = Methods.GetChildGroups(new List<Group> { group }, _unitOfWork.Groups);

                    groupFullInfo = new GroupFullInfo { GroupId = group.GroupId, BasicName = basicGroupName, FullName = result1.Item2, ParentIds = result1.Item1, ChildIds = result2 };
                    groupsFullInfoDictionary.Add(result1.Item1[0], groupFullInfo);
                    groupsFullInfoList.Add(groupFullInfo);
                }

                return Ok(groupsFullInfoList);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Nadpisuje pojedyncze wystąpienie <see cref="Group"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID grupy</param>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanego wystąpienia</param>
        /// <param name="connectionId">ID połączenia z centrum SignalR</param>
        /// <returns>Nadpisane zażądane wystąpienie <see cref="Group"/></returns>
        /// <response code="200">Nadpisane zażądane wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie grupy;
        /// plan zajęć jest aktualnie zablokowany;
        /// nie zostało podane ID połączenia;
        /// nie zostały odnalezione wymagane dane z bazy;
        /// nie zostały zablokowane wymagane edycje zajęć w bazie (dotyczące wybranej grupy studenckiej);
        /// nie zostały zablokowane wymagane pozycje harmonogramu w bazie (dotyczące zablokowanych edycji zajęć);
        /// wykryty został konflikt w planie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult UpdateGroup([FromODataUri] int key, [FromBody] Delta<Group> delta, [FromQuery] string connectionId)
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
                    var originGroup = _unitOfWork.Groups
                    .GetFirst(e => e.GroupId == key).Result;
                    if (originGroup == null)
                    {
                        return NotFound();
                    }

                    if (!delta.GetChangedPropertyNames().Contains("ParentGroupId"))
                    {
                        delta.Patch(originGroup);

                        _unitOfWork.Complete();

                        return Ok(originGroup);
                    }

                    var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);
                    if (connectionId == null)
                    {
                        return BadRequest("Connection id not found.");
                    }

                    delta.TryGetPropertyValue("ParentGroupId", out var destinationGroupIdObject);
                    var destinationGroupId = (int?) destinationGroupIdObject;

                    if (originGroup.ParentGroupId == destinationGroupId)
                    {
                        return BadRequest("Destination group cannot be the direct parent of origin group.");
                    }

                    Group destinationGroup = null;
                    if (destinationGroupId != null)
                    {
                        destinationGroup = _unitOfWork.Groups.Get(e => e.GroupId == destinationGroupId).FirstOrDefault();
                        if (destinationGroup == null)
                        {
                            return BadRequest("Could not find destination group.");
                        }
                    }

                    var childGroupsIds = Methods.GetChildGroups(new List<Group>() { originGroup }, _unitOfWork.Groups);
                    var parentGroupsIds = destinationGroup != null ? Methods.GetParentGroups(new List<Group>() { destinationGroup }, _unitOfWork.Groups) : new List<int>();

                    if (destinationGroupId != null)
                    {
                        if (childGroupsIds.Contains((int)destinationGroupId))
                        {
                            return BadRequest("Destination group cannot be the child of origin group.");
                        }
                    }

                    var groupsIds = childGroupsIds.Union(parentGroupsIds).ToList();

                    var courseEditionQueues = new SortedList<CourseEditionKey, ConcurrentQueue<object>>();

                    var originCourseEditionKeys = _unitOfWork.GroupCourseEditions
                        .Get(e => childGroupsIds.Contains(e.GroupId)).Select(e => new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }).ToList();

                    var destinationCourseEditionKeys = _unitOfWork.GroupCourseEditions
                        .Get(e => parentGroupsIds.Contains(e.GroupId)).Select(e => new CourseEditionKey
                        {
                            CourseId = e.CourseId,
                            CourseEditionId = e.CourseEditionId
                        }).ToList();

                    if (originCourseEditionKeys.Intersect(destinationCourseEditionKeys).Any())
                    {
                        return BadRequest("Origin and destination groups have common course editions.");
                    }

                    var courseEditionKeys = originCourseEditionKeys.Union(destinationCourseEditionKeys).ToList();

                    lock (ScheduleHub.CourseEditionLocks)
                    {
                        ScheduleHub.AddCourseEditionsLocks(courseEditionKeys, courseEditionQueues);
                    }

                    ScheduleHub.EnterQueues(courseEditionQueues.Values);
                    try
                    {
                        var notLockedCourseEditions = _unitOfWork.GroupCourseEditions
                            .Get(e => groupsIds.Contains(e.GroupId)
                                && (e.CourseEdition.LockUserId != userId || e.CourseEdition.LockUserConnectionId != connectionId))
                            .Include(e => e.CourseEdition)
                            .ToList();

                        if (notLockedCourseEditions.Any())
                        {
                            return BadRequest("You did not lock all groups course editions.");
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
                            var originCourseEditionIds = originCourseEditionKeys.Select(e => e.CourseEditionId).ToList();
                            var originTimestamps = _unitOfWork.SchedulePositions
                                .Get(e => originCourseEditionIds.Contains(e.CourseEditionId))
                                .Include(e => e.Timestamp)
                                .Select(e => e.Timestamp)
                                .ToList();

                            var destinationCourseEditionIds = destinationCourseEditionKeys.Select(e => e.CourseEditionId).ToList();
                            var destinationTimestamps = _unitOfWork.SchedulePositions
                                .Get(e => destinationCourseEditionIds.Contains(e.CourseEditionId))
                                .Include(e => e.Timestamp)
                                .Select(e => e.Timestamp)
                                .ToList();

                            var intersectedTimestamps = originTimestamps.Intersect(destinationTimestamps).ToList();
                            if (intersectedTimestamps.Any())
                            {
                                var timestamps = string.Join(", ", intersectedTimestamps);
                                return BadRequest($"Conflict detected for Timestamps ({timestamps}).");
                            }

                            //update
                            delta.Patch(originGroup);

                            _unitOfWork.Complete();

                            return Ok(originGroup);
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
        /// Usuwa pojedyncze wystąpienie <see cref="Group"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID grupy</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z grupą ze względu na posiadanie przez nią grup podrzędnych;
        /// nie udało się usunąć danych związanych z grupą ze względu na wystąpienie z nią powiązań w edycjach zajęć;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteGroup([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.Groups
                    .Delete(e => e.GroupId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                var subGroups = await _unitOfWork.Groups
                    .Get(e => e.ParentGroupId == key).FirstOrDefaultAsync();

                if (subGroups != null)
                {
                    return BadRequest("You cannot remove this group because it contains some child groups.");
                }

                var courseEditions = await _unitOfWork.GroupCourseEditions
                    .Get(e => e.GroupId == key).FirstOrDefaultAsync();

                if (courseEditions != null)
                {
                    return BadRequest("You cannot remove this group because there are some course editions assigned to it.");
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
        /// Usuwa wszystkie wystąpienia <see cref="Group"/> i powiązane z nimi dane
        /// - wystąpienia <see cref="StudentGroup"/>.
        /// </summary>
        /// <returns>Informację o tym ile rekordów w bazie zostało usuniętych</returns>
        /// <response code="200">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć danych związanych z grupami ze względu na wystąpienie z nimi powiązań w edycjach zajęć;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.ClearGroups")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ClearGroups()
        {
            try
            {
                var courseEditions = _unitOfWork.GroupCourseEditions.GetAll();
                if (courseEditions.Any())
                {
                    return BadRequest("You cannot clear groups because there are some course editions assigned to them.");
                }

                int studentGroupsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [StudentGroups]");
                int groupsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [Groups]");

                return Ok(new { StudentGroupsAffected = studentGroupsAffected, GroupsAffected = groupsAffected });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
