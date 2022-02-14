using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="StudentGroup"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("StudentGroups")]
    public class StudentGroupsController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public StudentGroupsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Tworzy nowe wystąpienie <see cref="StudentGroup"/>.
        /// </summary>
        /// <param name="studentGroupDto">Obiekt transferu danych</param>
        /// <returns>Nowo utworzone wystąpienie <see cref="StudentGroup"/></returns>
        /// <response code="201">Zwrócono nowo utworzone wystąpienie</response>
        /// <response code="400">
        /// Błędne dane w obiekcie transferu; 
        /// plan zajęć jest aktualnie zablokowany;
        /// wybrany użytkownik nie posiada roli studenta;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie udało się dodać nowo utworzonego wystąpienia do bazy danych</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
        [ODataRoute("")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult CreateStudentGroup([FromBody] StudentGroupDto studentGroupDto)
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
                    var user = _unitOfWork.Users
                        .GetFirst(e => e.UserId == studentGroupDto.StudentId).Result;

                    if (user == null || !user.IsStudent)
                    {
                        return BadRequest("Could not find user with given ID or user is not a student.");
                    }

                    var _student = _unitOfWork.StudentGroups.Add(studentGroupDto.FromDto()).Result;

                    if (_student != null)
                    {
                        _unitOfWork.Complete();
                        return Created(_student);
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
        /// Zwraca wszystkie wystąpienia <see cref="StudentGroup"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="StudentGroup"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetStudentGroups()
        {
            return Ok(_unitOfWork.StudentGroups.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="StudentGroup"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key1">ID grupy</param>
        /// <param name="key2">ID studenta (użytkownika)</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="StudentGroup"/></returns>
        /// <response code="200">Zwrócono żądane wystąpienie</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("{key1},{key2}")]
        [CustomEnableQuery]
        [ODataRoute("({key1},{key2})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetStudentGroup([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _studentGroup = _unitOfWork.StudentGroups.Get(e => e.GroupId == key1 && e.StudentId == key2);
                if (!_studentGroup.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_studentGroup));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca listę grup, do których należy użytkownik.
        /// </summary>
        /// <returns>Listę grup, do których należy użytkownik</returns>
        /// <response code="200">Zwrócono listę grup, do których należy użytkownik</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        [Authorize]
        [HttpGet("Service.GetMyGroups()")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetMyGroups()
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);

                var _myGroups = _unitOfWork.StudentGroups
                    .Get(e => e.StudentId == userId)
                    .Include(e => e.Group)
                    .Select(e => e.Group)
                    .ToList();

                var parentGroupIds = Methods.GetParentGroups(_myGroups, _unitOfWork.Groups).Distinct();

                var allMyGroups = _unitOfWork.Groups
                    .Get(e => parentGroupIds.Contains(e.GroupId))
                    .ToList();

                return Ok(_myGroups);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca listę studentów dla podanych grup.
        /// </summary>
        /// <param name="GroupsIds">Kolekcja identyfikatorów grup studenckich</param>
        /// <returns>Listę studentów, którzy należą do podanych grup</returns>
        /// <response code="200">Zwrócono listę studentów, którzy należą do danych grup</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("Service.GetGroupsStudents({GroupsIds})")]
        [CustomEnableQuery]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetGroupsStudents([FromODataUri] IEnumerable<int> GroupsIds)
        {
            try
            {
                if (!GroupsIds.Any())
                {
                    return Ok(Enumerable.Empty<User>());
                }

                var mainGroupId = GroupsIds.First();
                var otherGroupIds = GroupsIds.Except(new List<int> { mainGroupId });
                var studentIds = _unitOfWork.StudentGroups
                    .Get(e => GroupsIds.Contains(e.GroupId))
                    .GroupBy(e => e.StudentId)
                    .Select(e => e.Key)
                    .ToList();

                var _students = _unitOfWork.Users
                    .Get(e => studentIds.Contains(e.UserId))
                    .ToList();

                return Ok(_students);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Nadaje lub zabiera rolę starosty grupy studentowi (użytkownikowi).
        /// </summary>
        /// <param name="parameters">Wymagane parametry akcji OData
        /// - UserId (integer): identyfikator użytkownika posiadającego rolę studenta
        /// - GroupId (integer): identyfikator grupy studenckiej
        /// - Role (boolean): prawda - nadanie roli starosty, fałsz - odebranie roli starosty</param>
        /// <returns>Informację o powodzeniu operacji</returns>
        /// <response code="200"></response>
        /// <response code="400">
        /// Błędne dane przekazane jako parametry żądania;
        /// plan zajęć jest aktualnie zablokowany;
        /// użytkownik nie posiada roli studenta w systemie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.GiveOrRemoveRepresentativeRole")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GiveOrRemoveRepresentativeRole(ODataActionParameters parameters)
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
                    int userId = (int)parameters["UserId"];
                    int groupId = (int)parameters["GroupId"];
                    bool role = (bool)parameters["Role"];

                    var user = _unitOfWork.Users
                        .GetFirst(e => e.UserId == userId).Result;

                    if (user == null || !user.IsStudent)
                    {
                        return BadRequest("Could not find user with given ID or user is not a student.");
                    }

                    var studentGroup = _unitOfWork.StudentGroups
                        .Get(e => e.StudentId == userId && e.GroupId == groupId)
                        .FirstOrDefault();

                    if (studentGroup == null && !role)
                    {
                        return Ok();
                    }
                    if (studentGroup == null)
                    {
                        var assign = new StudentGroup { GroupId = groupId, StudentId = userId, IsRepresentative = true };
                        var result = _unitOfWork.StudentGroups.Add(assign).Result;
                        
                        _unitOfWork.Complete();
                        
                        return Ok();
                    }
                    else
                    {
                        studentGroup.IsRepresentative = role;
                        _unitOfWork.StudentGroups.Update(studentGroup);
                         
                        _unitOfWork.Complete();
                        
                        return Ok();
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
        /// Nadpisuje pojedyncze wystąpienie <see cref="StudentGroup"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key1">ID grupy</param>
        /// <param name="key2">ID studenta (użytkownika)</param>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanego wystąpienia</param>
        /// <returns>Nadpisane zażądane wystąpienie <see cref="StudentGroup"/></returns>
        /// <response code="200">Nadpisane zażądane wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie przypisania studenta do grupy;
        /// plan zajęć jest aktualnie zablokowany;
        /// użytkownik nie posiada roli studenta w systemie;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch("{key1},{key2}")]
        [ODataRoute("({key1},{key2})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult UpdateStudentGroup([FromODataUri] int key1, [FromODataUri] int key2, [FromBody] Delta<StudentGroup> delta)
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
                    var user = _unitOfWork.Users
                        .GetFirst(e => e.UserId == key2).Result;

                    if (user == null || !user.IsStudent)
                    {
                        return BadRequest("Could not find user with given ID or user is not a student.");
                    }

                    var _studentGroup = _unitOfWork.StudentGroups.GetFirst(e => e.GroupId == key1 && e.StudentId == key2).Result;
                    if (_studentGroup == null)
                    {
                        return NotFound();
                    }

                    delta.Patch(_studentGroup);

                    _unitOfWork.Complete();

                    return Ok(_studentGroup);
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
        /// Usuwa pojedyncze wystąpienie <see cref="StudentGroup"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key1">ID grupy</param>
        /// <param name="key2">ID studenta (użytkownika)</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięcie powiodło się</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key1},{key2}")]
        [ODataRoute("({key1},{key2})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteStudentGroup([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var result = await _unitOfWork.StudentGroups
                    .Delete(e => e.GroupId == key1 && e.StudentId == key2);
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

        /// <summary>
        /// Usuwa wszystkie wystąpienia <see cref="StudentGroup"/>.
        /// </summary>
        /// <returns>Informację o tym ile rekordów w bazie zostało usuniętych</returns>
        /// <response code="200">Usunięcie powiodło się</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.ClearStudentGroups")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult ClearStudentGroups()
        {
            try
            {
                int studentGroupsAffected = _unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [StudentGroups]");

                return Ok(new {StudentGroupsAffected = studentGroupsAffected });
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
