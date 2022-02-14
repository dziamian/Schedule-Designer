using ScheduleDesigner.Authentication;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.UnitOfWork
{
    /// <summary>
    /// Implementacja wzorca Unit of Work.
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        /// <summary>
        /// Kontekst połączenia z bazą danych.
        /// </summary>
        public ScheduleDesignerDbContext Context { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Authorization"/>.
        /// </summary>
        public IAuthorizationRepo Authorizations { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="CoordinatorCourseEdition"/>.
        /// </summary>
        public ICoordinatorCourseEditionRepo CoordinatorCourseEditions { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="CourseEdition"/>.
        /// </summary>
        public ICourseEditionRepo CourseEditions { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Course"/>.
        /// </summary>
        public ICourseRepo Courses { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="CourseRoom"/>.
        /// </summary>
        public ICourseRoomRepo CourseRooms { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="CourseType"/>.
        /// </summary>
        public ICourseTypeRepo CourseTypes { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="GroupCourseEdition"/>.
        /// </summary>
        public IGroupCourseEditionRepo GroupCourseEditions { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Group"/>.
        /// </summary>
        public IGroupRepo Groups { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Room"/>.
        /// </summary>
        public IRoomRepo Rooms { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="RoomType"/>.
        /// </summary>
        public IRoomTypeRepo RoomTypes { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="ScheduledMove"/>.
        /// </summary>
        public IScheduledMoveRepo ScheduledMoves { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="ScheduledMovePosition"/>.
        /// </summary>
        public IMessageRepo Messages { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Message"/>.
        /// </summary>
        public IScheduledMovePositionRepo ScheduledMovePositions { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="SchedulePosition"/>.
        /// </summary>
        public ISchedulePositionRepo SchedulePositions { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Settings"/>.
        /// </summary>
        public ISettingsRepo Settings { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="StudentGroup"/>.
        /// </summary>
        public IStudentGroupRepo StudentGroups { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Timestamp"/>.
        /// </summary>
        public ITimestampRepo Timestamps { get; private set; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="User"/>.
        /// </summary>
        public IUserRepo Users { get; private set; }

        /// <summary>
        /// Konstruktor wzorca Unit of Work.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
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

        /// <summary>
        /// Metoda wywołująca zapis zmian w kontekście bazy danych.
        /// </summary>
        /// <returns>Liczbę zmian zapisanych przez kontekst</returns>
        public int Complete()
        {
            return Context.SaveChanges();
        }

        /// <summary>
        /// Metoda wywołująca asynchroniczny zapis zmian w kontekście bazy danych.
        /// </summary>
        /// <returns>Asynchroniczną operację przechowującą liczbę zmian zapisanych przez kontekst</returns>
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
