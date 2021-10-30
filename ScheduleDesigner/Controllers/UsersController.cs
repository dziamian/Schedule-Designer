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
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Hubs.Interfaces;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Services;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Users")]
    public class UsersController : ODataController
    {
        private readonly IUserRepo _userRepo;
        private readonly UsosAuthenticationService _usosService;
        
        public UsersController(IUserRepo userRepo, UsosAuthenticationService usosService)
        {
            _userRepo = userRepo;
            _usosService = usosService;
        }

        private static bool IsDataValid(User user)
        {
            return user.Student != null || user.Coordinator != null || user.Staff != null;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateUser([FromBody] User user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!IsDataValid(user))
            {
                ModelState.AddModelError("UserRoles", "User must have assigned at least one role.");
                return BadRequest(ModelState);
            }

            try
            {
                var _user = await _userRepo.Add(user);

                if (_user != null)
                {
                    await _userRepo.SaveChanges();
                    return Created(_user);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateMyAccount()
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);

                var _user = _userRepo.Get(e => e.UserId == userId);
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
                    await _userRepo.SaveChanges();
                    return Created(user);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize]
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
                var _user = _userRepo.Get(e => e.UserId == userId);
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
                    await _userRepo.SaveChanges();
                    return Created(user);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [EnableQuery(PageSize = 20)]
        [ODataRoute("")]
        public IActionResult GetUsers()
        {
            return Ok(_userRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetUser([FromODataUri] int key)
        {
            try
            {
                var _user = _userRepo.Get(e => e.UserId == key);
                if (!_user.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_user));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [Authorize]
        [EnableQuery]
        [HttpGet]
        public IActionResult GetMyAccount()
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id").Value);
                var _user = _userRepo
                    .Get(e => e.UserId == userId);
                
                if (!_user.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_user));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

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
                var _user = await _userRepo.GetFirst(e => e.UserId == key);
                if (_user == null)
                {
                    return NotFound();
                }

                delta.Patch(_user);

                await _userRepo.SaveChanges();

                return Ok(_user);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteUser([FromODataUri] int key)
        {
            try
            {
                var result = await _userRepo.Delete(e => e.UserId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                await _userRepo.SaveChanges();

                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
