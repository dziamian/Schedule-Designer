using EntityFrameworkCore.Testing.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using ScheduleDesigner.Controllers;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;

namespace Testing
{
    /// <summary>
    /// Klasa zawierająca testy jednostkowe dotyczące pokojów.
    /// </summary>
    public class RoomsTests
    {
        /// <summary>
        /// Fałszywy kontekst połączenia z bazą danych.
        /// </summary>
        private ScheduleDesignerDbContext mockDbContext;

        /// <summary>
        /// Fałszywa implementacja wzorca UoW.
        /// </summary>
        private Mock<UnitOfWork> mockUnitOfWork;

        /// <summary>
        /// Fałszywa tożsamość użytkownika.
        /// </summary>
        private GenericPrincipal mockUser;

        /// <summary>
        /// Metoda przygotowująca dane niezbędne do wykonania testów jednostkowych.
        /// Tworzy fałszywy kontekst połączenia z bazą danych i zasila go.
        /// </summary>
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

            var courseEditions = new List<CourseEdition>
            {
                new CourseEdition { CourseEditionId = 1, CourseId = 1, Name = "CE1" },
                new CourseEdition { CourseEditionId = 2, CourseId = 1, Name = "CE2" }
            };

            var positions = new List<SchedulePosition>
            {
                new SchedulePosition {RoomId = 1, TimestampId = 1, CourseId = 1, CourseEditionId = 1},
                new SchedulePosition {RoomId = 3, TimestampId = 1, CourseId = 1, CourseEditionId = 2},
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
            mockDbContext.Set<CourseEdition>().AddRange(courseEditions);
            mockDbContext.Set<SchedulePosition>().AddRange(positions);

            mockDbContext.SaveChanges();
        }

        /// <summary>
        /// Metoda tworząca fałszywą tożsamość użytkownika.
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
        /// Metoda wywoływana przed każdym testem jednostkowym przygotowująca dane, 
        /// fałszywą tożsamość użytkownika i fałszywą implementację wzorca UoW.
        /// </summary>
        [SetUp]
        public void Setup()
        {
            PrepareData();
            PrepareUser();

            mockUnitOfWork = new Mock<UnitOfWork>(mockDbContext);
        }

        /// <summary>
        /// Test jednostkowy polegający na sprawdzeniu, czy funkcja <see cref="SchedulePositionsController.GetRoomsAvailibility"/>
        /// zwraca poprawne rezultaty - wszystkie zwrócone pokoje powinny być zajęte.
        /// </summary>
        [Test]
        public void GetRoomsAvailibility_ShouldNotBeAvailable()
        {
            var httpContext = new DefaultHttpContext
            {
                User = mockUser
            };
            var controller = new SchedulePositionsController(mockUnitOfWork.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            var result = controller.GetRoomsAvailibility(new List<int> {1, 3}, 1, 1, new int[] { 1, 2, 3 });
            var okResult = result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);

            CollectionAssert.AreEquivalent(new List<RoomAvailability>
            {
                new RoomAvailability {RoomId = 1, IsBusy = true},
                new RoomAvailability {RoomId = 3, IsBusy = true}
            }, ((Dictionary<int, RoomAvailability>.ValueCollection)okResult.Value).ToList());
        }

        /// <summary>
        /// Test jednostkowy polegający na sprawdzeniu, czy funkcja <see cref="SchedulePositionsController.GetRoomsAvailibility"/>
        /// zwraca poprawne rezultaty - wszystkie zwrócone pokoje powinny być dostępne.
        /// </summary>
        [Test]
        public void GetRoomsAvailibility_ShouldBeAvailable()
        {
            var httpContext = new DefaultHttpContext
            {
                User = mockUser
            };
            var controller = new SchedulePositionsController(mockUnitOfWork.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = httpContext
                }
            };

            var result = controller.GetRoomsAvailibility(new List<int> { 2 }, 1, 1, new int[] { 1 });
            var okResult = result as OkObjectResult;

            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);

            CollectionAssert.AreEquivalent(new List<RoomAvailability>
            {
                new RoomAvailability {RoomId = 2, IsBusy = false},
            }, ((Dictionary<int, RoomAvailability>.ValueCollection)okResult.Value).ToList());
        }
    }
}
