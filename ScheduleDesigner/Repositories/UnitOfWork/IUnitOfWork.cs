using ScheduleDesigner.Authentication;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.UnitOfWork
{
    /// <summary>
    /// Interfejs wzorca Unit of Work.
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Kontekst połączenia z bazą danych.
        /// </summary>
        ScheduleDesignerDbContext Context { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Authorization"/>.
        /// </summary>
        IAuthorizationRepo Authorizations { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="CoordinatorCourseEdition"/>.
        /// </summary>
        ICoordinatorCourseEditionRepo CoordinatorCourseEditions { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="CourseEdition"/>.
        /// </summary>
        ICourseEditionRepo CourseEditions { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Course"/>.
        /// </summary>
        ICourseRepo Courses { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="CourseRoom"/>.
        /// </summary>
        ICourseRoomRepo CourseRooms { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="CourseType"/>.
        /// </summary>
        ICourseTypeRepo CourseTypes { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="GroupCourseEdition"/>.
        /// </summary>
        IGroupCourseEditionRepo GroupCourseEditions { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Group"/>.
        /// </summary>
        IGroupRepo Groups { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Room"/>.
        /// </summary>
        IRoomRepo Rooms { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="RoomType"/>.
        /// </summary>
        IRoomTypeRepo RoomTypes { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="ScheduledMove"/>.
        /// </summary>
        IScheduledMoveRepo ScheduledMoves { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="ScheduledMovePosition"/>.
        /// </summary>
        IScheduledMovePositionRepo ScheduledMovePositions { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Message"/>.
        /// </summary>
        IMessageRepo Messages { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="SchedulePosition"/>.
        /// </summary>
        ISchedulePositionRepo SchedulePositions { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Settings"/>.
        /// </summary>
        ISettingsRepo Settings { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="StudentGroup"/>.
        /// </summary>
        IStudentGroupRepo StudentGroups { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="Timestamp"/>.
        /// </summary>
        ITimestampRepo Timestamps { get; }

        /// <summary>
        /// Interfejs rozszerzonego repozytorium dla modelu <see cref="User"/>.
        /// </summary>
        IUserRepo Users { get; }

        /// <summary>
        /// Metoda wywołująca zapis zmian w kontekście bazy danych.
        /// </summary>
        /// <returns>Liczbę zmian zapisanych przez kontekst</returns>
        int Complete();

        /// <summary>
        /// Metoda wywołująca asynchroniczny zapis zmian w kontekście bazy danych.
        /// </summary>
        /// <returns>Asynchroniczną operację przechowującą liczbę zmian zapisanych przez kontekst</returns>
        Task<int> CompleteAsync();
    }
}
