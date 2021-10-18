using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Timestamps")]
    public class TimestampsController : ControllerBase
    {
        private readonly ITimestampRepo _timestampRepo;

        public TimestampsController(ITimestampRepo timestampRepo, ISettingsRepo settingsRepo)
        {
            _timestampRepo = timestampRepo;
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("")]
        public IActionResult GetTimestamps()
        {
            return Ok(_timestampRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetCourse([FromODataUri] int key)
        {
            try
            {
                var _timestamp = _timestampRepo.Get(e => e.TimestampId == key);
                if (!_timestamp.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_timestamp));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
