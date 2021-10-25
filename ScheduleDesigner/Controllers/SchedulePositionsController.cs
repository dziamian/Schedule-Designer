using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("SchedulePositions")]
    public class SchedulePositionsController : ODataController
    {
        [HttpGet]
        public async Task<IActionResult> GetFreePeriods()
        {
            return Ok(new int[] { 1, 1, 2, 2, 4, 4, 4, 5 });
        }
    }
}
