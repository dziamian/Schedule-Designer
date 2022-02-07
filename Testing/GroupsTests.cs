using EntityFrameworkCore.Testing.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using ScheduleDesigner.Controllers;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Helpers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Security.Principal;

namespace Testing
{
    public class GroupsTests
    {
        private ScheduleDesignerDbContext mockDbContext;
        private Mock<UnitOfWork> mockUnitOfWork;
        private GenericPrincipal mockUser;

        private void PrepareData()
        {
            var group1 = new Group { GroupId = 1, Name = "A", ParentGroupId = null };
            var group2 = new Group { GroupId = 2, Name = "B", ParentGroupId = 1 };
            var group3 = new Group { GroupId = 3, Name = "C", ParentGroupId = 1 };
            var group4 = new Group { GroupId = 4, Name = "D", ParentGroupId = 3 };
            var group5 = new Group { GroupId = 5, Name = "E", ParentGroupId = 3 };

            var groups = new List<Group>() { group1, group2, group3, group4, group5 }.ToList();

            mockDbContext = Create.MockedDbContextFor<ScheduleDesignerDbContext>();
            mockDbContext.Set<Group>().AddRange(groups);
            mockDbContext.SaveChanges();
        }

        private void PrepareUser()
        {
            var claims = new List<Claim>
            {
                new Claim("user_id", "1")
            };

            var identity = new ClaimsIdentity(claims, "mock");
            mockUser = new GenericPrincipal(identity, null);

        }

        [SetUp]
        public void Setup()
        {
            PrepareData();
            PrepareUser();

            mockUnitOfWork = new Mock<UnitOfWork>(mockDbContext);
        }

        [Test]
        public void GetGroupFullName_ShouldBeOkTest()
        {
            var httpContext = new DefaultHttpContext
            {
                User = mockUser
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

            Assert.AreEqual("ACD", fullNameObject.FullName);
            Assert.AreEqual(2, fullNameObject.Levels);
        }

        [Test]
        public void GetGroupFullName_ShouldBeNotFoundTest()
        {
            var httpContext = new DefaultHttpContext
            {
                User = mockUser
            };
            var controller = new GroupsController(mockUnitOfWork.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            var result = controller.GetGroupFullName(6).Result;
            var notFoundResult = result as NotFoundResult;

            Assert.IsNotNull(notFoundResult);
            Assert.AreEqual(404, notFoundResult.StatusCode);
        }

        [Test]
        public void GetParentGroups_Test1()
        {
            var group = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 5).FirstOrDefault();
            var group2 = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 2).FirstOrDefault();

            Assert.IsNotNull(group);
            Assert.IsNotNull(group2);

            var results = Methods.GetParentGroups(new List<Group> { group, group2 }, mockUnitOfWork.Object.Groups);

            CollectionAssert.AreEquivalent(new List<int> { 5, 2, 1, 3 }, results.Distinct());
        }

        [Test]
        public void GetParentGroups_Test2()
        {
            var group = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 1).FirstOrDefault();

            Assert.IsNotNull(group);

            var results = Methods.GetParentGroups(new List<Group> { group }, mockUnitOfWork.Object.Groups);

            CollectionAssert.AreEquivalent(new List<int> { 1 }, results.Distinct());
        }

        [Test]
        public void GetParentGroups_Test3()
        {
            var group = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 3).FirstOrDefault();
            var group2 = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 4).FirstOrDefault();
            var group3 = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 5).FirstOrDefault();

            Assert.IsNotNull(group);
            Assert.IsNotNull(group2);
            Assert.IsNotNull(group3);

            var results = Methods.GetParentGroups(new List<Group> { group, group2, group3 }, mockUnitOfWork.Object.Groups);

            CollectionAssert.AreEquivalent(new List<int> { 3, 4, 5, 1 }, results.Distinct());
        }

        [Test]
        public void GetChildGroups_Test1()
        {
            var group = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 5).FirstOrDefault();
            var group2 = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 2).FirstOrDefault();

            Assert.IsNotNull(group);
            Assert.IsNotNull(group2);

            var results = Methods.GetChildGroups(new List<Group> { group, group2 }, mockUnitOfWork.Object.Groups);

            CollectionAssert.AreEquivalent(new List<int> { 5, 2 }, results.Distinct());
        }

        [Test]
        public void GetChildGroups_Test2()
        {
            var group = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 1).FirstOrDefault();

            Assert.IsNotNull(group);

            var results = Methods.GetChildGroups(new List<Group> { group }, mockUnitOfWork.Object.Groups);

            CollectionAssert.AreEquivalent(new List<int> { 1, 2, 3, 4, 5 }, results.Distinct());
        }

        [Test]
        public void GetChildGroups_Test3()
        {
            var group = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 3).FirstOrDefault();
            var group2 = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 4).FirstOrDefault();
            var group3 = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 5).FirstOrDefault();

            Assert.IsNotNull(group);
            Assert.IsNotNull(group2);
            Assert.IsNotNull(group3);

            var results = Methods.GetChildGroups(new List<Group> { group, group2, group3 }, mockUnitOfWork.Object.Groups);

            CollectionAssert.AreEquivalent(new List<int> { 3, 4, 5 }, results.Distinct());
        }
    }
}