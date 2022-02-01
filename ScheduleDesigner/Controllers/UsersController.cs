using System;
using System.Collections.Generic;
using System.Linq;
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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet]
        [CustomEnableQuery]
        public IActionResult GetOtherUsers()
        {
            try
            {
                var users = _unitOfWork.Users
                    .Get(e => e.Student == null && e.Coordinator == null && e.Staff == null)
                    .Include(e => e.Student)
                    .Include(e => e.Coordinator)
                    .Include(e => e.Staff)
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
        public async Task<IActionResult> UpdateUser([FromODataUri] int key, [FromBody] Delta<User> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _user = await _unitOfWork.Users.GetFirst(e => e.UserId == key);
                if (_user == null)
                {
                    return NotFound();
                }

                delta.Patch(_user);

                await _unitOfWork.CompleteAsync();

                return Ok(_user);
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
