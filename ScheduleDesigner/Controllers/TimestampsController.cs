using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Timestamps")]
    public class TimestampsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public TimestampsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetTimestamps()
        {
            return Ok(_unitOfWork.Timestamps.GetAll());
        }

        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetTimestamp([FromODataUri] int key)
        {
            try
            {
                var _timestamp = _unitOfWork.Timestamps.Get(e => e.TimestampId == key);
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
