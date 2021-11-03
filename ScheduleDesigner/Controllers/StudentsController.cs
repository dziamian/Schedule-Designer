﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Controllers
{
    [ODataRoutePrefix("Students")]
    public class StudentsController : ODataController
    {
        private readonly IUserRepo _userRepo;
        private readonly IStudentRepo _studentRepo;

        public StudentsController(IUserRepo userRepo, IStudentRepo studentRepo)
        {
            _userRepo = userRepo;
            _studentRepo = studentRepo;
        }

        private static bool IsDataValid(User user)
        {
            return user.Coordinator != null || user.Staff != null;
        }

        [HttpPost]
        [ODataRoute("")]
        public async Task<IActionResult> CreateStudent([FromBody] Student student)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _student = await _studentRepo.Add(student);

                if (_student != null)
                {
                    await _studentRepo.SaveChanges();
                    return Created(_student);
                }
                return NotFound();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet]
        [EnableQuery(PageSize = 20)]
        [ODataRoute("")]
        public IActionResult GetStudents()
        {
            return Ok(_studentRepo.GetAll());
        }

        [HttpGet]
        [EnableQuery]
        [ODataRoute("({key})")]
        public IActionResult GetStudent([FromODataUri] int key)
        {
            try
            {
                var _student = _studentRepo.Get(e => e.UserId == key);
                if (!_student.Any())
                {
                    return NotFound();
                }

                return Ok(SingleResult.Create(_student));
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPatch]
        [ODataRoute("({key})")]
        public async Task<IActionResult> UpdateStudent([FromODataUri] int key, [FromBody] Delta<Student> delta)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var _student = await _studentRepo.GetFirst(e => e.UserId == key);
                if (_student == null)
                {
                    return NotFound();
                }

                delta.Patch(_student);

                await _studentRepo.SaveChanges();

                return Ok(_student);
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpDelete]
        [ODataRoute("({key})")]
        public async Task<IActionResult> DeleteStudent([FromODataUri] int key)
        {
            try
            {
                var _user = _userRepo
                    .Get(e => e.UserId == key)
                    .Include(e => e.Student)
                    .Include(e => e.Coordinator)
                    .Include(e => e.Staff);

                if (!IsDataValid(_user.First()))
                {
                    return BadRequest("You cannot remove the only existing role for this user.");
                }

                var result = await _studentRepo.Delete(e => e.UserId == key);
                if (result < 0)
                {
                    return NotFound();
                }

                await _studentRepo.SaveChanges();
                return NoContent();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}