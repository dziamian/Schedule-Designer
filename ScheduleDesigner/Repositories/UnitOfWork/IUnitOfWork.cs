using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        ScheduleDesignerDbContext Context { get; }
        IAuthorizationRepo Authorizations { get; }
        ICoordinatorCourseEditionRepo CoordinatorCourseEditions { get; }
        ICourseEditionRepo CourseEditions { get; }
        ICourseRepo Courses { get; }
        ICourseRoomRepo CourseRooms { get; }
        ICourseTypeRepo CourseTypes { get; }
        IGroupCourseEditionRepo GroupCourseEditions { get; }
        IGroupRepo Groups { get; }
        IRoomRepo Rooms { get; }
        IRoomTypeRepo RoomTypes { get; }
        IScheduledMoveRepo ScheduledMoves { get; }
        IScheduledMovePositionRepo ScheduledMovePositions { get; }
        IMessageRepo Messages { get; }
        ISchedulePositionRepo SchedulePositions { get; }
        ISettingsRepo Settings { get; }
        IStudentGroupRepo StudentGroups { get; }
        ITimestampRepo Timestamps { get; }
        IUserRepo Users { get; }

        int Complete();
        Task<int> CompleteAsync();
    }
}
