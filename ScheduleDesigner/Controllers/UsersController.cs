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
    [ODataRoutePrefix("Users")]
    public class UsersController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UsosAuthenticationService _usosService;
        
        public UsersController(IUnitOfWork unitOfWork, UsosAuthenticationService usosService)
        {
            _unitOfWork = unitOfWork;
            _usosService = usosService;
        }

        [Authorize]
        [HttpPost]
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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPost]
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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetUsers()
        {
            return Ok(_unitOfWork.Users.GetAll());
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
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

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        public IActionResult GetCoordinators()
        {
            return Ok(_unitOfWork.Users.Get(e => e.IsCoordinator).ToList());
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
        [CustomEnableQuery]
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

        [Authorize]
        [CustomEnableQuery]
        [HttpGet]
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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpPatch]
        [ODataRoute("({key})")]
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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpDelete]
        [ODataRoute("({key})")]
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
