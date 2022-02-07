using EntityFrameworkCore.Testing.Moq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Moq;
using NUnit.Framework;
using ScheduleDesigner.Hubs;
using ScheduleDesigner.Hubs.Helpers;
using ScheduleDesigner.Hubs.Interfaces;
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
    public class ScheduleTests
    {
        private const string mockConnectionId = "123";
        private ScheduleDesignerDbContext mockDbContext;
        private Mock<HubCallerContext> mockClientContext;
        private Mock<IScheduleClient> mockClientProxy;
        private Mock<IHubCallerClients<IScheduleClient>> mockClients;
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
                new Room { RoomId = 4, RoomTypeId = 1, Name = "4", Capacity = 50},
            };

            var courseTypes = new List<CourseType>
            {
                new CourseType { CourseTypeId = 1, Name = "Type" }
            };
            var courses = new List<Course>
            {
                new Course { CourseId = 1, CourseTypeId = 1, Name = "Course", UnitsMinutes = 240 }
            };
            var courseRooms = new List<CourseRoom>
            {
                new CourseRoom {RoomId = 1, CourseId = 1},
                new CourseRoom {RoomId = 2, CourseId = 1},
                new CourseRoom {RoomId = 3, CourseId = 1},
            };

            var users = new List<User>
            {
                new User { UserId = 1, FirstName = "A", LastName = "A" },
                new User { UserId = 2, FirstName = "B", LastName = "B" }
            };
            var coordinators = new List<Coordinator>
            {
                new Coordinator { UserId = 1, TitleBefore = "A", TitleAfter = "A" },
                new Coordinator { UserId = 2, TitleBefore = "B", TitleAfter = "B" }
            };
            var groups = new List<Group>
            {
                new Group { GroupId = 1, Name = "A", ParentGroupId = null },
                new Group { GroupId = 2, Name = "B", ParentGroupId = null }
            };
            var courseEditions = new List<CourseEdition>
            {
                new CourseEdition { CourseEditionId = 1, CourseId = 1, Name = "CE1", LockUserId = 2, LockUserConnectionId = mockConnectionId },
                new CourseEdition { CourseEditionId = 2, CourseId = 1, Name = "CE2", LockUserId = 1, LockUserConnectionId = mockConnectionId },
                new CourseEdition { CourseEditionId = 3, CourseId = 1, Name = "CE3", LockUserId = 1, LockUserConnectionId = mockConnectionId },
                new CourseEdition { CourseEditionId = 4, CourseId = 1, Name = "CE4" },
            };
            var groupCourseEditions = new List<GroupCourseEdition>
            {
                new GroupCourseEdition { CourseId = 1, CourseEditionId = 1, GroupId = 1 },
                new GroupCourseEdition { CourseId = 1, CourseEditionId = 2, GroupId = 2 },
                new GroupCourseEdition { CourseId = 1, CourseEditionId = 3, GroupId = 2 },
                new GroupCourseEdition { CourseId = 1, CourseEditionId = 4, GroupId = 1 }
            };
            var coordinatorCourseEditions = new List<CoordinatorCourseEdition>
            {
                new CoordinatorCourseEdition { CourseId = 1, CourseEditionId = 1, CoordinatorId = 1 },
                new CoordinatorCourseEdition { CourseId = 1, CourseEditionId = 1, CoordinatorId = 2 },
                new CoordinatorCourseEdition { CourseId = 1, CourseEditionId = 2, CoordinatorId = 2 },
                new CoordinatorCourseEdition { CourseId = 1, CourseEditionId = 3, CoordinatorId = 1 },
                new CoordinatorCourseEdition { CourseId = 1, CourseEditionId = 4, CoordinatorId = 2 },
            };

            var positions = new List<SchedulePosition>
            {
                new SchedulePosition {RoomId = 1, TimestampId = 1, CourseId = 1, CourseEditionId = 2, LockUserId = 2, LockUserConnectionId = mockConnectionId},
                new SchedulePosition {RoomId = 2, TimestampId = 3, CourseId = 1, CourseEditionId = 2, LockUserId = 1, LockUserConnectionId = mockConnectionId},
                new SchedulePosition {RoomId = 1, TimestampId = 2, CourseId = 1, CourseEditionId = 3},
                new SchedulePosition {RoomId = 3, TimestampId = 4, CourseId = 1, CourseEditionId = 4}
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
            mockDbContext.Set<Coordinator>().AddRange(coordinators);
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
                new Claim("user_id", "1"),
                new Claim(ClaimTypes.Role, "Administrator")
            };

            var identity = new ClaimsIdentity(claims, "mock");
            mockUser = new GenericPrincipal(identity, null);

        }

        private void PrepareHub()
        {
            mockClients = new Mock<IHubCallerClients<IScheduleClient>>();
            mockClientProxy = new Mock<IScheduleClient>();
            mockClientContext = new Mock<HubCallerContext>();
            
            mockClients.Setup(clients => clients.Caller).Returns(mockClientProxy.Object);
            mockClients.Setup(clients => clients.Others).Returns(mockClientProxy.Object);
            mockClients.Setup(clients => clients.All).Returns(mockClientProxy.Object);

            mockClientContext.Setup(c => c.ConnectionId).Returns(mockConnectionId);
            mockClientContext.Setup(c => c.User).Returns(mockUser);
        }

        [SetUp]
        public void Setup()
        {
            PrepareData();
            PrepareUser();
            PrepareHub();

            mockUnitOfWork = new Mock<UnitOfWork>(mockDbContext);
        }

        [Test]
        public void AddSchedulePositions_ShouldBeLockedByOtherUser()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.AddSchedulePositions(1, 1, 1, 1, 1, new int[] { 1 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject 
            { 
                StatusCode = 400, 
                Message = "You didn't lock this course edition." 
            }), Times.AtLeastOnce);
        }

        [Test]
        public void AddSchedulePositions_TooManyUnitsMinutes()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.AddSchedulePositions(1, 2, 1, 1, 1, new int[] { 2 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject
            {
                StatusCode = 400,
                Message = "You cannot add this amount of units to the schedule."
            }), Times.AtLeastOnce);
        }

        [Test]
        public void AddSchedulePositions_ShouldNotBeDone()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.AddSchedulePositions(1, 3, 1, 1, 1, new int[] { 1 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject
            {
                StatusCode = 400,
                Message = "Some conflicts with other courses occurred."
            }), Times.AtLeastOnce);
        }

        [Test]
        public void AddSchedulePositions_ShouldBeDone()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.AddSchedulePositions(1, 3, 1, 1, 1, new int[] { 5 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject
            {
                StatusCode = 200,
            }), Times.AtLeastOnce);
        }

        [Test]
        public void ModifySchedulePositions_ShouldBeLockedByOtherUser()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.ModifySchedulePositions(1, 1, 1, new int[] { 1 }, 2, 1, 1, new int[] { 1 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject
            {
                StatusCode = 400,
                Message = "You didn't lock some positions in schedule."
            }), Times.AtLeastOnce);
        }

        [Test]
        public void ModifySchedulePositions_ShouldNotBeDone()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.ModifySchedulePositions(2, 1, 1, new int[] { 3 }, 2, 1, 1, new int[] { 2 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject
            {
                StatusCode = 400,
                Message = "Some conflicts with other courses occurred."
            }), Times.AtLeastOnce);
        }

        [Test]
        public void ModifySchedulePositions_ShouldBeDone()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.ModifySchedulePositions(2, 1, 1, new int[] { 3 }, 1, 1, 1, new int[] { 5 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject
            {
                StatusCode = 200,
            }), Times.AtLeastOnce);
        }

        [Test]
        public void RemoveSchedulePositions_ShouldBeLockedByOtherUser()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.RemoveSchedulePositions(1, 1, 1, new int[] { 1 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject
            {
                StatusCode = 400,
                Message = "You didn't lock some positions in schedule."
            }), Times.AtLeastOnce);
        }

        [Test]
        public void RemoveSchedulePositions_ShouldNotBeDone()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.RemoveSchedulePositions(1, 1, 1, new int[] { 1, 8 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject
            {
                StatusCode = 404,
                Message = "Could not find requested source time periods."
            }), Times.AtLeastOnce);
        }

        [Test]
        public void RemoveSchedulePositions_ShouldBeDone()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            hub.RemoveSchedulePositions(2, 1, 1, new int[] { 3 });

            mockClientProxy.Verify(x => x.SendResponse(new MessageObject
            {
                StatusCode = 204
            }), Times.AtLeastOnce);
        }

        [Test]
        public void AddScheduledMove_ShouldBeLockedByOtherUser()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            var response = hub.AddScheduledMove(1, 1, 1, new int[] { 1 }, 2, 1, 1, new int[] { 1 }, false, "");

            Assert.AreEqual(new MessageObject
            {
                StatusCode = 400,
                Message = "You didn't lock some positions in schedule."
            }, response);
        }

        [Test]
        public void AddScheduledMove_ShouldNotBeDone()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            var response = hub.AddScheduledMove(2, 1, 1, new int[] { 3 }, 1, 1, 1, new int[] { 5 }, false, "");

            Assert.AreEqual(new MessageObject
            {
                StatusCode = 400,
                Message = "This move is possible, so you should not try to schedule it."
            }, response);
        }

        [Test]
        public void AddScheduledMove_ShouldBeDone()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            var response = hub.AddScheduledMove(2, 1, 1, new int[] { 3 }, 2, 1, 1, new int[] { 2 }, false, "");

            Assert.AreEqual(new MessageObject
            {
                StatusCode = 200
            }, response);
        }

        [Test]
        public void AddScheduledMove_ShouldBeDoneProposition()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            var response = hub.AddScheduledMove(2, 1, 1, new int[] { 3 }, 2, 1, 1, new int[] { 2 }, true, "");

            Assert.AreEqual(new MessageObject
            {
                StatusCode = 200
            }, response);

            mockClientProxy.Verify(x => x.AddedScheduledMove(1, 1, false, 1, 2, 2, 1, 1, new int[] { 3 }), Times.AtLeastOnce);
        }

        [Test]
        public void AddScheduledMove_ShouldNotBeDoneChosenRoom()
        {
            var hub = new ScheduleHub(mockUnitOfWork.Object)
            {
                Clients = mockClients.Object,
                Context = mockClientContext.Object
            };

            var response = hub.AddScheduledMove(2, 1, 1, new int[] { 3 }, 4, 1, 1, new int[] { 2 }, true, "");

            Assert.AreEqual(new MessageObject
            {
                StatusCode = 400,
                Message = "Chosen room does not exist or has not been assigned to chosen course."
            }, response);
        }
    }
}
