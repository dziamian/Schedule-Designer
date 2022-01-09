using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ScheduleDesigner.Attributes;
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("GroupCourseEditions")]
    public class GroupCourseEditions : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public GroupCourseEditions(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        [CustomEnableQuery]
        [ODataRoute("")]
        public IActionResult GetGroupCourseEditions()
        {
            return Ok(_unitOfWork.GroupCourseEditions.GetAll());
        }
    }
}
