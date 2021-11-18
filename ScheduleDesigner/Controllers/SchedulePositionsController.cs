using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("SchedulePositions")]
    public class SchedulePositionsController : ODataController
    {
        private readonly ISchedulePositionRepo _schedulePositionRepo;

        public SchedulePositionsController(ISchedulePositionRepo schedulePositionRepo)
        {
            _schedulePositionRepo = schedulePositionRepo;
        }

        [Authorize(Policy = "Coordinator")]
        [HttpGet]
        [EnableQuery(MaxExpansionDepth = 4)]
        public IActionResult GetScheduleAsCoordinator([FromODataUri] IEnumerable<int> Weeks)
        {
            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);

                var _schedulePositions = _schedulePositionRepo
                    .Get(e => e.CourseEdition.Coordinators.Any(e => e.CoordinatorId == userId)
                              && Weeks.Contains(e.CourseRoomTimestamp.Timestamp.Week))
                    .Include(e => e.CourseEdition)
                    .ThenInclude(e => e.Coordinators)
                    .Include(e => e.CourseRoomTimestamp)
                    .ThenInclude(e => e.Timestamp);

                return Ok(_schedulePositions);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            } 
        }
    }
}
