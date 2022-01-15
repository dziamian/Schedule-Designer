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
    public class ScheduleController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ScheduleController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("export")]
        public IActionResult Export()
        {
            var scheduleData = _unitOfWork.SchedulePositions.GetAll().ToList();
            var fileDownloadName = "schedulePositions.csv";
            return new ScheduleCsvResult(scheduleData, fileDownloadName);
        }
    }
}
