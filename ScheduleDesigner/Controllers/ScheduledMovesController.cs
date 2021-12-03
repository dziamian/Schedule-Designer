using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("ScheduledMoves")]
    public class ScheduledMovesController : ODataController
    {
        private readonly IScheduledMoveRepo _scheduledMoveRepo;

        public ScheduledMovesController(IScheduledMoveRepo scheduledMoveRepo)
        {
            _scheduledMoveRepo = scheduledMoveRepo;
        }

        [HttpGet]
        [EnableQuery(MaxExpansionDepth = 3)]
        [ODataRoute("")]
        public IActionResult GetScheduledMoves()
        {
            return Ok(_scheduledMoveRepo.GetAll());
        }
    }
}
