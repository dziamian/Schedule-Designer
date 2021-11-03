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
            Random random = new Random();
            int x = random.Next() % 4 + 1;
            int y = random.Next() % 3 + 1;

            return Ok(new int[] { x, y, x + 1, y + 1 });
        }
    }
}
