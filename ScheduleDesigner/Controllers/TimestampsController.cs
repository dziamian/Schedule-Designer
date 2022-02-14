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
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Controllers
{
    /// <summary>
    /// Kontroler API przeznaczony do zarządzania <see cref="Timestamp"/>.
    /// </summary>
    [Route("api/[controller]")]
    [ApiExplorerSettings(IgnoreApi = false)]
    [ODataRoutePrefix("Timestamps")]
    public class TimestampsController : ControllerBase
    {
        /// <summary>
        /// Instancja klasy wzorca UoW.
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        /// <summary>
        /// Konstruktor kontrolera wykorzystujący wstrzykiwanie zależności.
        /// </summary>
        /// <param name="unitOfWork">Wstrzyknięta instancja klasy wzorca UoW</param>
        public TimestampsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Zwraca wszystkie wystąpienia <see cref="Timestamp"/>.
        /// </summary>
        /// <returns>Listę wystąpień <see cref="Timestamp"/></returns>
        /// <response code="200">Zwrócono listę wystąpień</response>
        [Authorize]
        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        [ProducesResponseType(200)]
        public IActionResult GetTimestamps()
        {
            return Ok(_unitOfWork.Timestamps.GetAll());
        }

        /// <summary>
        /// Zwraca pojedyncze wystąpienie <see cref="Timestamp"/> na podstawie jego ID.
        /// </summary>
        /// <param name="key">ID ramy czasowej</param>
        /// <returns>Znalezione pojedyncze wystąpienie <see cref="Timestamp"/></returns>
        /// <response code="200">Zwrócono żądane wystąpienie</response>
        /// <response code="400">Nastąpił nieprzewidziany błąd</response>
        /// <response code="404">Nie znaleziono żądanego wystąpienia</response>
        [Authorize]
        [HttpGet("{key}")]
        [CustomEnableQuery]
        [ODataRoute("({key})")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
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
                return BadRequest("Unexpected error. Please try again later.");
            }
        }
    }
}
