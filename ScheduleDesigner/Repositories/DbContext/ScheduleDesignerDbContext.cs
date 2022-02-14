using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Authentication;
using System.Data;
using Microsoft.Data.SqlClient;

namespace ScheduleDesigner.Repositories
{
    /// <summary>
    /// Klasa kontekstu połączenia z bazą danych.
    /// </summary>
    public class ScheduleDesignerDbContext : DbContext
    {
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="Authorization"/> w bazie danych.
        /// </summary>
        public virtual DbSet<Authorization> Authorizations { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="User"/> w bazie danych.
        /// </summary>
        public virtual DbSet<User> Users { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="Settings"/> w bazie danych.
        /// </summary>
        public virtual DbSet<Settings> Settings { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="CourseType"/> w bazie danych.
        /// </summary>
        public virtual DbSet<CourseType> CourseTypes { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="Course"/> w bazie danych.
        /// </summary>
        public virtual DbSet<Course> Courses { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="Group"/> w bazie danych.
        /// </summary>
        public virtual DbSet<Group> Groups { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="StudentGroup"/> w bazie danych.
        /// </summary>
        public virtual DbSet<StudentGroup> StudentGroups { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="CourseEdition"/> w bazie danych.
        /// </summary>
        public virtual DbSet<CourseEdition> CourseEditions { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="CoordinatorCourseEdition"/> w bazie danych.
        /// </summary>
        public virtual DbSet<CoordinatorCourseEdition> CoordinatorCourseEditions { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="GroupCourseEdition"/> w bazie danych.
        /// </summary>
        public virtual DbSet<GroupCourseEdition> GroupCourseEditions { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="RoomType"/> w bazie danych.
        /// </summary>
        public virtual DbSet<RoomType> RoomTypes { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="Room"/> w bazie danych.
        /// </summary>
        public virtual DbSet<Room> Rooms { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="CourseRoom"/> w bazie danych.
        /// </summary>
        public virtual DbSet<CourseRoom> CourseRooms { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="Timestamp"/> w bazie danych.
        /// </summary>
        public virtual DbSet<Timestamp> Timestamps { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="SchedulePosition"/> w bazie danych.
        /// </summary>
        public virtual DbSet<SchedulePosition> SchedulePositions { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="ScheduledMovePosition"/> w bazie danych.
        /// </summary>
        public virtual DbSet<ScheduledMovePosition> ScheduledMovePositions { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="ScheduledMove"/> w bazie danych.
        /// </summary>
        public virtual DbSet<ScheduledMove> ScheduledMoves { get; set; }
        /// <summary>
        /// Reprezentacja kolekcji instancji <see cref="Message"/> w bazie danych.
        /// </summary>
        public virtual DbSet<Message> Messages { get; set; }

        /// <summary>
        /// Konstruktor kontekstu połączenia z bazą danych.
        /// </summary>
        /// <param name="options">Opcje konfigurujące kontekst</param>
        public ScheduleDesignerDbContext(DbContextOptions<ScheduleDesignerDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Authorization
            modelBuilder.Entity<Authorization>()
                .HasKey(e => e.UserId);

            modelBuilder.Entity<Authorization>()
                .Property(e => e.UserId)
                .ValueGeneratedNever();

            //User
            modelBuilder.Entity<User>()
                .HasKey(e => e.UserId);

            modelBuilder.Entity<User>()
                .Property(e => e.UserId)
                .ValueGeneratedNever();

            //Group
            modelBuilder.Entity<Group>()
                .HasOne(e => e.ParentGroup)
                .WithMany(e => e.SubGroups)
                .OnDelete(DeleteBehavior.Restrict);

            //StudentGroup
            modelBuilder.Entity<StudentGroup>()
                .HasKey(e => new { e.GroupId, e.StudentId });

            //CourseEdition
            modelBuilder.Entity<CourseEdition>()
                .HasKey(e => new { e.CourseId, e.CourseEditionId });

            modelBuilder.Entity<CourseEdition>()
                .Property(e => e.CourseEditionId)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            modelBuilder.Entity<CourseEdition>().
                HasIndex(e => e.CourseEditionId)
                .IsUnique();

            //CoordinatorCourseEdition
            modelBuilder.Entity<CoordinatorCourseEdition>()
                .HasKey(e => (new { e.CourseId, e.CourseEditionId, e.CoordinatorId }));

            modelBuilder.Entity<CoordinatorCourseEdition>()
                .HasOne(e => e.User)
                .WithMany(e => e.CourseEditions)
                .OnDelete(DeleteBehavior.Restrict);

            //GroupCourseEdition
            modelBuilder.Entity<GroupCourseEdition>()
                .HasKey(e => new { e.CourseId, e.CourseEditionId, e.GroupId });

            modelBuilder.Entity<GroupCourseEdition>()
                .HasOne(e => e.Group)
                .WithMany(e => e.CourseEditions)
                .OnDelete(DeleteBehavior.Restrict);

            //CourseRoom
            modelBuilder.Entity<CourseRoom>()
                .HasKey(e => new { e.RoomId, e.CourseId });

            //Timestamp
            modelBuilder.Entity<Timestamp>()
                .HasIndex(p => new { SlotIndex = p.PeriodIndex, p.Day, p.Week })
                .IsUnique(true);

            //SchedulePosition
            modelBuilder.Entity<SchedulePosition>()
                .HasKey(e => new { e.RoomId, e.TimestampId, e.CourseId });

            modelBuilder.Entity<SchedulePosition>()
                .HasMany(e => e.ScheduledMovePositions)
                .WithOne(e => e.Origin)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SchedulePosition>()
                .HasOne(e => e.CourseEdition)
                .WithMany(e => e.SchedulePositions)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SchedulePosition>()
                .HasOne(e => e.CourseRoom)
                .WithMany(e => e.SchedulePositions)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SchedulePosition>()
                .HasOne(e => e.Timestamp)
                .WithMany(e => e.SchedulePositions)
                .OnDelete(DeleteBehavior.Restrict);

            //ScheduledMovePosition
            modelBuilder.Entity<ScheduledMovePosition>()
                .HasKey(e => new { e.MoveId, e.RoomId_1, e.TimestampId_1, e.RoomId_2, e.TimestampId_2, e.CourseId });

            modelBuilder.Entity<ScheduledMovePosition>()
                .HasOne(e => e.DestinationRoom)
                .WithMany(e => e.ScheduledMoves)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ScheduledMovePosition>()
                .HasOne(e => e.DestinationTimestamp)
                .WithMany(e => e.ScheduledMoves)
                .OnDelete(DeleteBehavior.Restrict);

            //Message
            modelBuilder.Entity<Message>()
                .HasKey(e => e.MoveId);

            modelBuilder.Entity<Message>()
                .Property(e => e.MoveId)
                .ValueGeneratedNever();

            base.OnModelCreating(modelBuilder);
        }
    }
}
