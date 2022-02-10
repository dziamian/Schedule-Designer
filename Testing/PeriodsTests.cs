using EntityFrameworkCore.Testing.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ScheduleDesigner.Controllers;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace Testing
{
    public class PeriodsTests
    {
        private ScheduleDesignerDbContext mockDbContext;
        private Mock<UnitOfWork> mockUnitOfWork;
        private GenericPrincipal mockUser;

        private void PrepareData()
        {
            var timestamps = new List<Timestamp> 
            { 
                new Timestamp { TimestampId = 1, PeriodIndex = 1, Day = 1, Week = 1 },
                new Timestamp { TimestampId = 2, PeriodIndex = 1, Day = 1, Week = 2 },
                new Timestamp { TimestampId = 3, PeriodIndex = 1, Day = 1, Week = 3 },
                new Timestamp { TimestampId = 4, PeriodIndex = 1, Day = 1, Week = 4 },
                new Timestamp { TimestampId = 5, PeriodIndex = 1, Day = 1, Week = 5 },
                new Timestamp { TimestampId = 6, PeriodIndex = 1, Day = 1, Week = 6 }
            };

            var roomTypes = new List<RoomType>
            {
                new RoomType { RoomTypeId = 1, Name = "ABC" }
            };
            var rooms = new List<Room>
            {
                new Room { RoomId = 1, RoomTypeId = 1, Name = "1", Capacity = 1},
                new Room { RoomId = 2, RoomTypeId = 1, Name = "2", Capacity = 10},
                new Room { RoomId = 3, RoomTypeId = 1, Name = "3", Capacity = 20},
            };

            var courseTypes = new List<CourseType>
            {
                new CourseType { CourseTypeId = 1, Name = "Type" }
            };
            var courses = new List<Course>
            {
                new Course { CourseId = 1, CourseTypeId = 1, Name = "Course", UnitsMinutes = 900 }
            };
            var courseRooms = new List<CourseRoom>
            {
                new CourseRoom {RoomId = 1, CourseId = 1},
                new CourseRoom {RoomId = 2, CourseId = 1},
                new CourseRoom {RoomId = 3, CourseId = 1},
            };

            var users = new List<User>
            {
                new User { UserId = 1, FirstName = "A", LastName = "A", IsCoordinator = true, TitleBefore = "A", TitleAfter = "A" },
                new User { UserId = 2, FirstName = "B", LastName = "B", IsCoordinator = true, TitleBefore = "B", TitleAfter = "B" }
            };
            var groups = new List<Group>
            {
                new Group { GroupId = 1, Name = "A", ParentGroupId = null },
                new Group { GroupId = 2, Name = "B", ParentGroupId = null }
            };
            var courseEditions = new List<CourseEdition>
            {
                new CourseEdition { CourseEditionId = 1, CourseId = 1, Name = "CE1" },
                new CourseEdition { CourseEditionId = 2, CourseId = 1, Name = "CE2" }
            };
            var groupCourseEditions = new List<GroupCourseEdition>
            {
                new GroupCourseEdition { CourseId = 1, CourseEditionId = 1, GroupId = 1 },
                new GroupCourseEdition { CourseId = 1, CourseEditionId = 2, GroupId = 2 }
            };
            var coordinatorCourseEditions = new List<CoordinatorCourseEdition>
            {
                new CoordinatorCourseEdition { CourseId = 1, CourseEditionId = 1, CoordinatorId = 1 },
                new CoordinatorCourseEdition { CourseId = 1, CourseEditionId = 1, CoordinatorId = 2 },
                new CoordinatorCourseEdition { CourseId = 1, CourseEditionId = 2, CoordinatorId = 2 }
            };

            var positions = new List<SchedulePosition>
            {
                new SchedulePosition {RoomId = 1, TimestampId = 1, CourseId = 1, CourseEditionId = 1},
                new SchedulePosition {RoomId = 2, TimestampId = 2, CourseId = 1, CourseEditionId = 2},
            };

            mockDbContext = Create.MockedDbContextFor<ScheduleDesignerDbContext>();

            mockDbContext.Set<Settings>().Add(new Settings
            {
                CourseDurationMinutes = 120,
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(22),
                TermDurationWeeks = 15
            });
            mockDbContext.Set<Timestamp>().AddRange(timestamps);
            mockDbContext.Set<RoomType>().AddRange(roomTypes);
            mockDbContext.Set<Room>().AddRange(rooms);
            mockDbContext.Set<CourseType>().AddRange(courseTypes);
            mockDbContext.Set<Course>().AddRange(courses);
            mockDbContext.Set<CourseRoom>().AddRange(courseRooms);
            mockDbContext.Set<User>().AddRange(users);
            mockDbContext.Set<Group>().AddRange(groups);
            mockDbContext.Set<CourseEdition>().AddRange(courseEditions);
            mockDbContext.Set<GroupCourseEdition>().AddRange(groupCourseEditions);
            mockDbContext.Set<CoordinatorCourseEdition>().AddRange(coordinatorCourseEditions);
            mockDbContext.Set<SchedulePosition>().AddRange(positions);

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
        public void IsPeriodBusy_ShouldBeBusyTest()
        {
            var httpContext = new DefaultHttpContext
            {
                User = mockUser
            };
            var controller = new CourseEditionsController(mockUnitOfWork.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            var result = controller.IsPeriodBusy(1, 1, 1, 1, new int[] { 1, 2, 3 }).Result;
            var okResult = result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);

            Assert.True((bool)okResult.Value);
        }

        [Test]
        public void IsPeriodBusy_ShouldBeNotBusyTest()
        {
            var httpContext = new DefaultHttpContext
            {
                User = mockUser
            };
            var controller = new CourseEditionsController(mockUnitOfWork.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            var result = controller.IsPeriodBusy(1, 1, 1, 1, new int[] { 4, 5, 6 }).Result;
            var okResult = result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);

            Assert.False((bool)okResult.Value);
        }

        [Test]
        public void IsPeriodBusy_ShouldBeNotFoundTest()
        {
            var httpContext = new DefaultHttpContext
            {
                User = mockUser
            };
            var controller = new CourseEditionsController(mockUnitOfWork.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            var result = controller.IsPeriodBusy(3, 4, 1, 1, new int[] { 4, 5, 6 }).Result;
            var notFoundResult = result as NotFoundResult;

            Assert.IsNotNull(notFoundResult);
            Assert.AreEqual(404, notFoundResult.StatusCode);
        }

        [Test]
        public void IsPeriodBusy_ShouldBeNotBusyTest2()
        {
            var httpContext = new DefaultHttpContext
            {
                User = mockUser
            };
            var controller = new CourseEditionsController(mockUnitOfWork.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            var result = controller.IsPeriodBusy(1, 1, 1, 1, new int[] { 10,11,12 }).Result;
            var okResult = result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);

            Assert.False((bool)okResult.Value);
        }

        [Test]
        public void GetPeriods_LabelsShouldBeCorrectTest()
        {
            var httpContext = new DefaultHttpContext
            {
                User = mockUser
            };
            var controller = new SettingsController(mockUnitOfWork.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            var result = controller.GetPeriods().Result;
            var okResult = result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);

            CollectionAssert.AreEqual(new string[] { "10:00", "12:00", "14:00", "16:00", "18:00", "20:00", "22:00" }, (string[])okResult.Value);
        }
    }
}
