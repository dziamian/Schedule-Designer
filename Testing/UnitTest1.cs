using EntityFrameworkCore.Testing.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ScheduleDesigner.Controllers;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Testing
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void Test1()
        {
            var grandParentGroup = new Group() { GroupId = 1, Name = "A", ParentGroupId = null };
            var parentGroup = new Group() { GroupId = 2, Name = "B", ParentGroupId = 1, ParentGroup = grandParentGroup };
            var childGroup1 = new Group() { GroupId = 3, Name = "C", ParentGroupId = 2, ParentGroup = parentGroup };
            var childGroup2 = new Group() { GroupId = 4, Name = "D", ParentGroupId = 2, ParentGroup = parentGroup };
            
            var groups = new List<Group>() { grandParentGroup, parentGroup, childGroup1, childGroup2 }.ToList();

            var claims = new List<Claim>
            {
                new Claim("user_id", "1")
            };

            var identity = new ClaimsIdentity(claims, "mock");
            var user = new System.Security.Principal.GenericPrincipal(identity, null);

            var mockDbContext = Create.MockedDbContextFor<ScheduleDesignerDbContext>();
            mockDbContext.Set<Group>().AddRange(groups);
            mockDbContext.SaveChanges();

            var mockUnitOfWork = new Mock<SqlUnitOfWork>(mockDbContext);

            var httpContext = new DefaultHttpContext
            {
                User = user
            };
            var controller = new GroupsController(mockUnitOfWork.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            var result = controller.GetGroupFullName(4).Result;
            var okResult = result as OkObjectResult;
            
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            
            var fullNameObject = okResult.Value as GroupFullName;

            Assert.AreEqual("ABD", fullNameObject.FullName);
            Assert.AreEqual(2, fullNameObject.Levels);
        }
    }
}