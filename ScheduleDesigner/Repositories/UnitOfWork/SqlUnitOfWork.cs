﻿using ScheduleDesigner.Repositories.Interfaces;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.UnitOfWork
{
    public class SqlUnitOfWork : IUnitOfWork
    {
        private readonly ScheduleDesignerDbContext _context;

        public IAuthorizationRepo Authorizations { get; private set; }

        public ICoordinatorCourseEditionRepo CoordinatorCourseEditions { get; private set; }

        public ICoordinatorRepo Coordinators { get; private set; }

        public ICourseEditionRepo CourseEditions { get; private set; }

        public ICourseRepo Courses { get; private set; }

        public ICourseRoomRepo CourseRooms { get; private set; }

        public ICourseTypeRepo CourseTypes { get; private set; }

        public IGroupCourseEditionRepo GroupCourseEditions { get; private set; }

        public IGroupRepo Groups { get; private set; }

        public IRoomRepo Rooms { get; private set; }

        public IRoomTypeRepo RoomTypes { get; private set; }

        public IScheduledMoveRepo ScheduledMoves { get; private set; }

        public IScheduledMovePositionRepo ScheduledMovePositions { get; private set; }

        public ISchedulePositionRepo SchedulePositions { get; private set; }

        public ISettingsRepo Settings { get; private set; }

        public IStaffRepo Staffs { get; private set; }

        public IStudentGroupRepo StudentGroups { get; private set; }

        public IStudentRepo Students { get; private set; }

        public ITimestampRepo Timestamps { get; private set; }

        public IUserRepo Users { get; private set; }

        public SqlUnitOfWork(ScheduleDesignerDbContext context)
        {
            _context = context;

            Authorizations = new SqlAuthorizationRepo(context);
            CoordinatorCourseEditions = new SqlCoordinatorCourseEdition(context);
            Coordinators = new SqlCoordinatorRepo(context);
            CourseEditions = new SqlCourseEditionRepo(context);
            Courses = new SqlCourseRepo(context);
            CourseRooms = new SqlCourseRoomRepo(context);
            CourseTypes = new SqlCourseTypeRepo(context);
            GroupCourseEditions = new SqlGroupCourseEditionRepo(context);
            Groups = new SqlGroupRepo(context);
            Rooms = new SqlRoomRepo(context);
            RoomTypes = new SqlRoomTypeRepo(context);
            ScheduledMoves = new SqlScheduledMoveRepo(context);
            ScheduledMovePositions = new SqlScheduledMovePositionRepo(context);
            SchedulePositions = new SqlSchedulePositionRepo(context);
            Settings = new SqlSettingsRepo(context);
            Staffs = new SqlStaffRepo(context);
            StudentGroups = new SqlStudentGroupRepo(context);
            Students = new SqlStudentRepo(context);
            Timestamps = new SqlTimestampRepo(context);
            Users = new SqlUserRepo(context);
        }

        public int Complete()
        {
            return _context.SaveChanges();
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
