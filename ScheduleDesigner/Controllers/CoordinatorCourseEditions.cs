using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Controllers
{

    [ODataRoutePrefix("CoordinatorCourseEditions")]
    public class CoordinatorCourseEditions : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public CoordinatorCourseEditions(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetCoordinatorCourseEditions()
        {
            return Ok(_unitOfWork.CoordinatorCourseEditions.GetAll());
        }
    }
}
