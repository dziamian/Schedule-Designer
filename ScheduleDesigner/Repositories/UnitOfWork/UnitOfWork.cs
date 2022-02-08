using ScheduleDesigner.Repositories.Interfaces;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        public ScheduleDesignerDbContext Context { get; private set; }

        public IAuthorizationRepo Authorizations { get; private set; }

        public ICoordinatorCourseEditionRepo CoordinatorCourseEditions { get; private set; }

        public ICourseEditionRepo CourseEditions { get; private set; }

        public ICourseRepo Courses { get; private set; }

        public ICourseRoomRepo CourseRooms { get; private set; }

        public ICourseTypeRepo CourseTypes { get; private set; }

        public IGroupCourseEditionRepo GroupCourseEditions { get; private set; }

        public IGroupRepo Groups { get; private set; }

        public IRoomRepo Rooms { get; private set; }

        public IRoomTypeRepo RoomTypes { get; private set; }

        public IScheduledMoveRepo ScheduledMoves { get; private set; }

        public IMessageRepo Messages { get; private set; }

        public IScheduledMovePositionRepo ScheduledMovePositions { get; private set; }

        public ISchedulePositionRepo SchedulePositions { get; private set; }

        public ISettingsRepo Settings { get; private set; }

        public IStudentGroupRepo StudentGroups { get; private set; }

        public ITimestampRepo Timestamps { get; private set; }

        public IUserRepo Users { get; private set; }

        public UnitOfWork(ScheduleDesignerDbContext context)
        {
            Context = context;

            Authorizations = new AuthorizationRepo(context);
            CoordinatorCourseEditions = new CoordinatorCourseEditionRepo(context);
            CourseEditions = new CourseEditionRepo(context);
            Courses = new CourseRepo(context);
            CourseRooms = new CourseRoomRepo(context);
            CourseTypes = new CourseTypeRepo(context);
            GroupCourseEditions = new GroupCourseEditionRepo(context);
            Groups = new GroupRepo(context);
            Rooms = new RoomRepo(context);
            RoomTypes = new RoomTypeRepo(context);
            ScheduledMoves = new ScheduledMoveRepo(context);
            Messages = new MessageRepo(context);
            ScheduledMovePositions = new ScheduledMovePositionRepo(context);
            SchedulePositions = new SchedulePositionRepo(context);
            Settings = new SettingsRepo(context);
            StudentGroups = new StudentGroupRepo(context);
            Timestamps = new TimestampRepo(context);
            Users = new UserRepo(context);
        }

        public int Complete()
        {
            return Context.SaveChanges();
        }

        public async Task<int> CompleteAsync()
        {
            return await Context.SaveChangesAsync();
        }

        public void Dispose()
        {
            Context.Dispose();
        }
    }
}
