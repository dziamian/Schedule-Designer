using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.UnitOfWork;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do eksportowania wielu rodzajów danych z bazy.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ExportController : ControllerBase
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public ExportController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Eksportuje pozycje w planie (<see cref="SchedulePosition"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("schedulePositions"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportSchedulePositions()
        {
            var data = _unitOfWork.SchedulePositions.GetAll().ToList();
            var fileDownloadName = "SchedulePositions.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje przypisania prowadzących do edycji zajęć (<see cref="CoordinatorCourseEdition"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("coordinatorCourseEditions"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportCoordinatorCourseEditions()
        {
            var data = _unitOfWork.CoordinatorCourseEditions.GetAll().ToList();
            var fileDownloadName = "CoordinatorCourseEditions.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje edycje zajęć (<see cref="CourseEdition"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("courseEditions"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportCourseEditions()
        {
            var data = _unitOfWork.CourseEditions.GetAll().ToList();
            var fileDownloadName = "CourseEditions.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje przypisania grup do edycji zajęć (<see cref="GroupCourseEdition"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("groupCourseEditions"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportGroupCourseEditions()
        {
            var data = _unitOfWork.GroupCourseEditions.GetAll().ToList();
            var fileDownloadName = "GroupCourseEditions.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje przypisania pokojów do przedmiotów (<see cref="CourseRoom"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("courseRooms"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportCourseRooms()
        {
            var data = _unitOfWork.CourseRooms.GetAll().ToList();
            var fileDownloadName = "CourseRooms.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje przedmioty (<see cref="Course"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("courses"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportCourses()
        {
            var data = _unitOfWork.Courses.GetAll().ToList();
            var fileDownloadName = "Courses.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje typy przedmiotów (<see cref="CourseType"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("courseTypes"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportCourseTypes()
        {
            var data = _unitOfWork.CourseTypes.GetAll().ToList();
            var fileDownloadName = "CourseTypes.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje grupy (<see cref="Group"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("groups"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportGroups()
        {
            var data = _unitOfWork.Groups.GetAll().ToList();
            var fileDownloadName = "Groups.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje pokoje (<see cref="Room"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("rooms"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportRooms()
        {
            var data = _unitOfWork.Rooms.GetAll().ToList();
            var fileDownloadName = "Rooms.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje typy pokojów (<see cref="RoomType"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("roomTypes"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportRoomTypes()
        {
            var data = _unitOfWork.RoomTypes.GetAll().ToList();
            var fileDownloadName = "RoomTypes.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje przypisania studentów do poszczególnych grup (<see cref="StudentGroup"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("studentGroups"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportStudentGroups()
        {
            var data = _unitOfWork.StudentGroups.GetAll().ToList();
            var fileDownloadName = "StudentGroups.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje ramy czasowe planu zajęć (<see cref="Timestamp"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("timestamps"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportTimestamps()
        {
            var data = _unitOfWork.Timestamps.GetAll().ToList();
            var fileDownloadName = "Timestamps.csv";
            return new CsvExport(data, fileDownloadName);
        }

        /// <summary>
        /// Eksportuje dane użytkowników (<see cref="User"/>) do pliku CSV.
        /// </summary>
        /// <returns>Plik CSV z zażądanymi danymi</returns>
        /// <response code="200">Zwrócono plik CSV z danymi</response>
        [Authorize(Policy = "AdministratorOnly")]
        [HttpGet("users"), DisableRequestSizeLimit]
        [ProducesResponseType(200)]
        public IActionResult ExportUsers()
        {
            var data = _unitOfWork.Users.GetAll().ToList();
            var fileDownloadName = "Users.csv";
            return new CsvExport(data, fileDownloadName);
        }
    }
}
