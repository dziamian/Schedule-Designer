﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ScheduleDesigner.Repositories.UnitOfWork;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("CourseRooms")]
    public class CourseRoomsController : ODataController
    {
        private readonly IUnitOfWork _unitOfWork;

        public CourseRoomsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost]
        [ODataRoute("")]
        [Authorize(Policy = "Assistant")]
        public async Task<IActionResult> CreateCourseRoom([FromBody] CourseRoom courseRoom)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = int.Parse(HttpContext.User.Claims.FirstOrDefault(x => x.Type == "user_id")?.Value!);
                var isAdmin = HttpContext.User.HasClaim(c => c.Type == ClaimTypes.Role && c.Value == "Administrator");

                if (!isAdmin)
                {
                    courseRoom.UserId = userId;
                } 
                else
                {
                    courseRoom.UserId = null;
                }

                var _courseRoom = await _unitOfWork.CourseRooms.Add(courseRoom);

                if (_courseRoom == null)
                {
                    return NotFound();
                }
                
                await _unitOfWork.CompleteAsync();
                return Created(_courseRoom);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [EnableQuery(PageSize = 20)]
        [ODataRoute("")]
        public IActionResult GetCourseRooms()
        {
            return Ok(_unitOfWork.CourseRooms.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key1},{key2})")]
        public IActionResult GetCourseRoom([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var _courseRoom = _unitOfWork.CourseRooms
                    .Get(e => e.CourseId == key1 && e.RoomId == key2);
                if (!_courseRoom.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_courseRoom));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> UpdateCourseRoom([FromODataUri] int key1, [FromODataUri] int key2, [FromBody] Delta<CourseRoom> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _courseRoom = await _unitOfWork.CourseRooms
                    .GetFirst(e => e.CourseId == key1 && e.RoomId == key2);
                if (_courseRoom == null)
                {
                    return NotFound();
                }

                delta.Patch(_courseRoom);

                await _unitOfWork.CompleteAsync();

                return Ok(_courseRoom);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key1},{key2})")]
        public async Task<IActionResult> DeleteCourseRoom([FromODataUri] int key1, [FromODataUri] int key2)
        {
            try
            {
                var result = await _unitOfWork.CourseRooms
                    .Delete(e => e.CourseId == key1 && e.RoomId == key2);
                if (result < 0)
                {
                    return NotFound();
                }

                await _unitOfWork.CompleteAsync();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
