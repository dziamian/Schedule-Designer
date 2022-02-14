using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using ScheduleDesigner.Services;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="User"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("Users")]
    public class UsersController : ODataController
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Instancja serwisu zapewniającego poprawną komunikację z zewnętrznym systemem USOS
        /// </summary>
        private readonly UsosAuthenticationService _usosService;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        /// <param name="usosService">Wstrzyknięta instancja serwisu do komunikacji z systemem USOS</param>
        public UsersController(IUnitOfWork unitOfWork, UsosAuthenticationService usosService)
        {
            _unitOfWork = unitOfWork;
            _usosService = usosService;
        }

        /// <summary>
        /// Tworzy nowe konto dla zalogowanego użytkownika, jeżeli jeszcze takiego nie posiada w systemie.
        /// </summary>
        /// <returns>Nowo utworzone konto w systemie (obiekt klasy <see cref="User"/>)</returns>
        /// <response code="201">Zwrócono informacje o nowo utworzonym koncie w systemie</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie udało się pomyślnie utworzyć nowego konta w systemie</response>
        /// <response code="409">Konto użytkownika już istnieje w systemie</response>
        [Authorize]
        [HttpPost("Service.CreateMyAccount")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> CreateMyAccount()
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);

                var _user = _unitOfWork.Users.Get(e => e.UserId == userId);
                if (_user.Any())
                {
                    return Conflict("User is already created.");
                }

                var accessToken = HttpContext.Request.Headers["AccessToken"];
                var accessTokenSecret = HttpContext.Request.Headers["AccessTokenSecret"];

                var user = await _usosService.CreateUser(
                    await _usosService.GetUserInfo(
                        _usosService.GetOAuthRequest(accessToken, accessTokenSecret)
                    )
                );

                if (user != null)
                {
                    await _usosService.CreateCredentials(userId, accessToken, accessTokenSecret);
                    await _unitOfWork.CompleteAsync();
                    return Created(user);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Tworzy nowe konto dla istniejącego użytkownika w systemie USOS.
        /// </summary>
        /// <param name="parameters">Wymagane parametry akcji OData
        /// - UserId (integer): identyfikator użytkownika w systemie USOS</param>
        /// <returns>Nowo utworzone konto w systemie (obiekt klasy <see cref="User"/>)</returns>
        /// <response code="201">Zwrócono informacje o nowo utworzonym koncie w systemie</response>
        /// <response code="400">
        /// Błędne dane przekazane jako parametry żądania;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie udało się pomyślnie utworzyć nowego konta w systemie</response>
        /// <response code="409">Konto użytkownika już istnieje w systemie</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost("Service.CreateAccountFromUsos")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> CreateAccountFromUsos(ODataActionParameters parameters)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                int userId = (int)parameters["UserId"];
                var _user = _unitOfWork.Users.Get(e => e.UserId == userId);
                if (_user.Any())
                {
                    return Conflict("User is already created.");
                }

                var accessToken = HttpContext.Request.Headers["AccessToken"];
                var accessTokenSecret = HttpContext.Request.Headers["AccessTokenSecret"];

                var user = await _usosService.CreateUser(
                    await _usosService.GetUserInfo(
                        _usosService.GetOAuthRequest(accessToken, accessTokenSecret), userId
                    )
                );

                if (user != null)
                {
                    await _usosService.CreateCredentials(userId, "", "", DateTime.Now.AddMinutes(-30));
                    await _unitOfWork.CompleteAsync();
                    return Created(user);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Wyszukuje użytkowników w systemie USOS na podstawie podanych kryteriów.
        /// </summary>
        /// <param name="Query">Kryteria wyszukiwania</param>
        /// <param name="PerPage">Liczba użytkowników na stronie</param>
        /// <param name="Start">Liczba użytkowników do pominięcia</param>
        /// <returns>Informacje o odnalezionych użytkownikach w postaci obiektu klasy <see cref="SearchItem"/></returns>
        /// <response code="200">Zwrócono informacje o odnalezionych użytkownikach</response>
        /// <response code="400">
        /// Błędne dane przekazane w parametrach żądania;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("Service.SearchForUserFromUsos({Query},{PerPage},{Start})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SearchForUserFromUsos(
            [FromODataUri] string Query, 
            [FromODataUri] int PerPage, 
            [FromODataUri] int Start)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);

                var accessToken = HttpContext.Request.Headers["AccessToken"];
                var accessTokenSecret = HttpContext.Request.Headers["AccessTokenSecret"];

                var userSearch = await _usosService.GetUserSearch(
                    _usosService.GetOAuthRequest(accessToken, accessTokenSecret),
                    Query,
                    PerPage,
                    Start
                );

                return Ok(userSearch);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="User"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="User"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetUsers()
        {
            return Ok(_unitOfWork.Users.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="User"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID użytkownika</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="User"/></returns>
        /// <response code="200">Zwrócono żądane wystąpienie</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("{key}")]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetUser([FromODataUri] int key)
        {
            try
            {
                var _user = _unitOfWork.Users.Get(e => e.UserId == key);
                if (!_user.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_user));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="User"/> będących prowadzącymi.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="User"/> będacych prowadzącymi</returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet("Service.GetCoordinators()")]
        [CustomEnableQuery]
        [ProducesResponseType(200)]
        public IActionResult GetCoordinators()
        {
            return Ok(_unitOfWork.Users.Get(e => e.IsCoordinator).ToList());
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="User"/> nieposiadających żadnej roli.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="User"/> nieposiadających żadnej roli</returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("Service.GetOtherUsers()")]
        [CustomEnableQuery]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public IActionResult GetOtherUsers()
        {
            try
            {
                var users = _unitOfWork.Users
                    .Get(e => !e.IsStudent && !e.IsCoordinator && !e.IsStaff)
                    .ToList();

                return Ok(users);
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Zwraca informacje o zalogowanym użytkowniku.
        /// </summary>
        /// <returns>Informacje o zalogowanym użytkowniku</returns>
        /// <response code="200">Zwrócono informacje o koncie użytkownika</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono konta użytkownika</response>
        [Authorize]
        [CustomEnableQuery]
        [HttpGet("Service.GetMyAccount()")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMyAccount()
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);
                var _user = _unitOfWork.Users
                    .Get(e => e.UserId == userId);
                
                if (!_user.Any())
                {
                    return NotFound();
                }

                var accessToken = HttpContext.Request.Headers["AccessToken"];
                var accessTokenSecret = HttpContext.Request.Headers["AccessTokenSecret"];

                var result = await _usosService.CreateCredentials(userId, accessToken, accessTokenSecret);
                if (result != null)
                {
                    await _unitOfWork.CompleteAsync();
                }

                return Ok(SingleResult.Create(_user));
            }
            catch (Exception e)
            {
                return BadRequest("Unexpected error. Please try again later.");
            }
        }

        /// <summary>
        /// Nadpisuje pojedyncze wystąpienie <see cref="User"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID użytkownika</param>
        /// <param name="delta">Obiekt śledzący zmiany dla wysłanego wystąpienia</param>
        /// <returns>Nadpisane zażądane wystąpienie <see cref="User"/></returns>
        /// <response code="200">Nadpisane zażądane wystąpienie</response>
        /// <response code="400">
        /// Nieprawidłowe dane w obiekcie użytkownika;
        /// plan zajęć jest aktualnie zablokowany;
        /// nie udało się usunąć roli prowadzącego ze względu na wystąpienie powiązań z edycjami zajęć;
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult UpdateUser([FromODataUri] int key, [FromBody] Delta<User> delta)
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
                    var _user = _unitOfWork.Users.GetFirst(e => e.UserId == key).Result;
                    if (_user == null)
                    {
                        return NotFound();
                    }

                    delta.Patch(_user);

                    if (!_user.IsStaff)
                    {
                        _user.IsAdmin = false;
                        _user.IsCoordinator = false;
                    }

                    if (_user.IsCoordinator || _user.IsAdmin)
                    {
                        _user.IsStaff = true;
                    }

                    if (!_user.IsCoordinator)
                    {
                        var courseEditions = _unitOfWork.CoordinatorCourseEditions
                            .Get(e => e.CoordinatorId == key).FirstOrDefault();

                        if (courseEditions != null)
                        {
                            return BadRequest("You cannot remove this user because there are some course editions assigned to him.");
                        }
                    }

                    if (!_user.IsStudent)
                    {
                        _unitOfWork.StudentGroups.DeleteMany(e => e.StudentId == key);
                    }

                    _unitOfWork.Complete();

                    return Ok(_user);
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
        /// Usuwa pojedyncze wystąpienie <see cref="User"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID użytkownika</param>
        /// <returns>Informację o powodzeniu procesu usunięcia</returns>
        /// <response code="204">Usunięcie powiodło się</response>
        /// <response code="400">
        /// Nie udało się usunąć użytkownika ze względu na wystąpienie jego powiązań z edycjami zajęć;
        /// nastąpił nieprzewidziany błąd
        /// </response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete("{key}")]
        [ODataRoute("({key})")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteUser([FromODataUri] int key)
        {
            try
            {
                var result = await _unitOfWork.Users.Delete(e => e.UserId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                var courseEditions = await _unitOfWork.CoordinatorCourseEditions
                    .Get(e => e.CoordinatorId == key).FirstOrDefaultAsync();

                if (courseEditions != null)
                {
                    return BadRequest("You cannot remove this user because there are some course editions assigned to him.");
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
