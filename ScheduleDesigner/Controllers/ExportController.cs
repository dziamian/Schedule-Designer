using Microsoft.AspNetCore.Authorization;
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

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("schedulePositions"), DisableRequestSizeLimit]
        public IActionResult ExportSchedulePositions()
        {
            var data = _unitOfWork.SchedulePositions.GetAll().ToList();
            var fileDownloadName = "SchedulePositions.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("coordinatorCourseEditions"), DisableRequestSizeLimit]
        public IActionResult ExportCoordinatorCourseEditions()
        {
            var data = _unitOfWork.CoordinatorCourseEditions.GetAll().ToList();
            var fileDownloadName = "CoordinatorCourseEditions.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("courseEditions"), DisableRequestSizeLimit]
        public IActionResult ExportCourseEditions()
        {
            var data = _unitOfWork.CourseEditions.GetAll().ToList();
            var fileDownloadName = "CourseEditions.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("groupCourseEditions"), DisableRequestSizeLimit]
        public IActionResult ExportGroupCourseEditions()
        {
            var data = _unitOfWork.GroupCourseEditions.GetAll().ToList();
            var fileDownloadName = "GroupCourseEditions.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("courseRooms"), DisableRequestSizeLimit]
        public IActionResult ExportCourseRooms()
        {
            var data = _unitOfWork.CourseRooms.GetAll().ToList();
            var fileDownloadName = "CourseRooms.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("courses"), DisableRequestSizeLimit]
        public IActionResult ExportCourses()
        {
            var data = _unitOfWork.Courses.GetAll().ToList();
            var fileDownloadName = "Courses.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("courseTypes"), DisableRequestSizeLimit]
        public IActionResult ExportCourseTypes()
        {
            var data = _unitOfWork.CourseTypes.GetAll().ToList();
            var fileDownloadName = "CourseTypes.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("groups"), DisableRequestSizeLimit]
        public IActionResult ExportGroups()
        {
            var data = _unitOfWork.Groups.GetAll().ToList();
            var fileDownloadName = "Groups.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("rooms"), DisableRequestSizeLimit]
        public IActionResult ExportRooms()
        {
            var data = _unitOfWork.Rooms.GetAll().ToList();
            var fileDownloadName = "Rooms.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("roomTypes"), DisableRequestSizeLimit]
        public IActionResult ExportRoomTypes()
        {
            var data = _unitOfWork.RoomTypes.GetAll().ToList();
            var fileDownloadName = "RoomTypes.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("studentGroups"), DisableRequestSizeLimit]
        public IActionResult ExportStudentGroups()
        {
            var data = _unitOfWork.StudentGroups.GetAll().ToList();
            var fileDownloadName = "StudentGroups.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("timestamps"), DisableRequestSizeLimit]
        public IActionResult ExportTimestamps()
        {
            var data = _unitOfWork.Timestamps.GetAll().ToList();
            var fileDownloadName = "Timestamps.csv";
            return new CsvExport(data, fileDownloadName);
        }

        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("users"), DisableRequestSizeLimit]
        public IActionResult ExportUsers()
        {
            var data = _unitOfWork.Users.GetAll().ToList();
            var fileDownloadName = "Users.csv";
            return new CsvExport(data, fileDownloadName);
        }
    }
}
