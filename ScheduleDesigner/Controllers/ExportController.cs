using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.UnitOfWork;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ExportController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        //Authorize - Administrator
        [HttpGet("schedulePositions"), DisableRequestSizeLimit]
        public IActionResult ExportSchedulePositions()
        {
            var scheduleData = _unitOfWork.SchedulePositions.GetAll().ToList();
            var fileDownloadName = "SchedulePositions.csv";
            return new CsvExport(scheduleData, fileDownloadName);
        }
    }
}
