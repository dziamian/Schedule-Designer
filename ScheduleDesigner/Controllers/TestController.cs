using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [AllowAnonymous]
        [HttpGet("test1")]
        public async Task<ActionResult> GetTest()
        {
            return Ok("okey");
        }

        [Authorize]
        [HttpGet("test2")]
        public async Task<ActionResult> GetTestAuth()
        {
            HttpContext.User.Claims.ToList().ForEach(i => Console.WriteLine("{0}\n", i));
            return Ok("okey");
        }
    }
}
