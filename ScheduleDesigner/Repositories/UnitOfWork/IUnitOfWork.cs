using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IAuthorizationRepo Authorizations { get; }
        ICoordinatorCourseEditionRepo CoordinatorCourseEditions { get; }
        ICoordinatorRepo Coordinators { get; }
        ICourseEditionRepo CourseEditions { get; }
        ICourseRepo Courses { get; }
        ICourseRoomRepo CourseRooms { get; }
        ICourseTypeRepo CourseTypes { get; }
        IGroupCourseEditionRepo GroupCourseEditions { get; }
        IGroupRepo Groups { get; }
        IRoomRepo Rooms { get; }
        IRoomTypeRepo RoomTypes { get; }
        IScheduledMoveRepo ScheduledMoves { get; }
        ISchedulePositionRepo SchedulePositions { get; }
        ISettingsRepo Settings { get; }
        IStaffRepo Staffs { get; }
        IStudentGroupRepo StudentGroups { get; }
        IStudentRepo Students { get; }
        ITimestampRepo Timestamps { get; }
        IUserRepo Users { get; }

        int Complete();
        Task<int> CompleteAsync();
    }
}
