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
    /// <summary>
    /// Klasa zawieraj¹ca testy jednostkowe dotycz¹ce grup studenckich.
    /// </summary>
    public class GroupsTests
    {
        /// <summary>
        /// Fa³szywy kontekst po³¹czenia z baz¹ danych.
        /// </summary>
        private ScheduleDesignerDbContext mockDbContext;

        /// <summary>
        /// Fa³szywa implementacja wzorca UoW.
        /// </summary>
        private Mock<UnitOfWork> mockUnitOfWork;

        /// <summary>
        /// Fa³szywa to¿samoœæ u¿ytkownika.
        /// </summary>
        private GenericPrincipal mockUser;

        /// <summary>
        /// Metoda przygotowuj¹ca dane niezbêdne do wykonania testów jednostkowych.
        /// Tworzy fa³szywy kontekst po³¹czenia z baz¹ danych i zasila go.
        /// </summary>
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

        /// <summary>
        /// Metoda tworz¹ca fa³szyw¹ to¿samoœæ u¿ytkownika.
        /// </summary>
        private void PrepareUser()
        {
            var claims = new List<Claim>
            {
                new Claim("user_id", "1")
            };

            var identity = new ClaimsIdentity(claims, "mock");
            mockUser = new GenericPrincipal(identity, null);

        }

        /// <summary>
        /// Metoda wywo³ywana przed ka¿dym testem jednostkowym przygotowuj¹ca dane, 
        /// fa³szyw¹ to¿samoœæ u¿ytkownika i fa³szyw¹ implementacjê wzorca UoW.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            PrepareData();
            PrepareUser();

            mockUnitOfWork = new Mock<UnitOfWork>(mockDbContext);
        }

        /// <summary>
        /// Test jednostkowy polegaj¹cy na sprawdzeniu, czy funkcja <see cref="GroupsController.GetGroupFullName"/>
        /// zwraca poprawne rezultaty - pe³na nazwa grupy powinna byæ "ACD", a poziom wêz³a powinien byæ równy 2.
        /// </summary>
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

        /// <summary>
        /// Test jednostkowy polegaj¹cy na sprawdzeniu, czy funkcja <see cref="GroupsController.GetGroupFullName"/>
        /// zwraca poprawne rezultaty - grupa o podanym identyfikatorze powinna zostaæ nieodnaleziona.
        /// </summary>
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

        /// <summary>
        /// Test jednostkowy polegaj¹cy na sprawdzeniu, czy funkcja <see cref="Methods.GetParentGroups"/>
        /// zwraca poprawne rezultaty dla ró¿nych rodzajów danych 
        /// - w tym przypadku grupa pierwsza posiada jednego rodzica, a druga grupa dwóch.
        /// </summary>
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

        /// <summary>
        /// Test jednostkowy polegaj¹cy na sprawdzeniu, czy funkcja <see cref="Methods.GetParentGroups"/>
        /// zwraca poprawne rezultaty dla ró¿nych rodzajów danych 
        /// - w tym przypadku grupa nie posiada ¿adnych rodziców.
        /// </summary>
        [Test]
        public void GetParentGroups_Test2()
        {
            var group = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 1).FirstOrDefault();

            Assert.IsNotNull(group);

            var results = Methods.GetParentGroups(new List<Group> { group }, mockUnitOfWork.Object.Groups);

            CollectionAssert.AreEquivalent(new List<int> { 1 }, results.Distinct());
        }

        /// <summary>
        /// Test jednostkowy polegaj¹cy na sprawdzeniu, czy funkcja <see cref="Methods.GetParentGroups"/>
        /// zwraca poprawne rezultaty dla ró¿nych rodzajów danych 
        /// - w tym przypadku grupy posiadaj¹ jednego wspólnego rodzica.
        /// </summary>
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

        /// <summary>
        /// Test jednostkowy polegaj¹cy na sprawdzeniu, czy funkcja <see cref="Methods.GetChildGroups"/>
        /// zwraca poprawne rezultaty dla ró¿nych rodzajów danych 
        /// - w tym przypadku grupy nie posiadaj¹ ¿adnych dzieci.
        /// </summary>
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

        /// <summary>
        /// Test jednostkowy polegaj¹cy na sprawdzeniu, czy funkcja <see cref="Methods.GetChildGroups"/>
        /// zwraca poprawne rezultaty dla ró¿nych rodzajów danych 
        /// - w tym przypadku powinny zostaæ zwrócone wszystkie istniej¹ce grupy.
        /// </summary>
        [Test]
        public void GetChildGroups_Test2()
        {
            var group = mockUnitOfWork.Object.Groups.Get(e => e.GroupId == 1).FirstOrDefault();

            Assert.IsNotNull(group);

            var results = Methods.GetChildGroups(new List<Group> { group }, mockUnitOfWork.Object.Groups);

            CollectionAssert.AreEquivalent(new List<int> { 1, 2, 3, 4, 5 }, results.Distinct());
        }

        /// <summary>
        /// Test jednostkowy polegaj¹cy na sprawdzeniu, czy funkcja <see cref="Methods.GetChildGroups"/>
        /// zwraca poprawne rezultaty dla ró¿nych rodzajów danych 
        /// - w tym przypadku jedna z wybranych grup jest dzieckiem innej, a oprócz tego nie posiadaj¹ ¿adnych innych dzieci.
        /// </summary>
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