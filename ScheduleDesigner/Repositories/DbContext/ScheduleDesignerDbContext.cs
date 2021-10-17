using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories
{
    public class ScheduleDesignerDbContext : DbContext
    {
        public DbSet<Settings> Settings { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Programme> Programmes { get; set; }
        public DbSet<ProgrammeStage> ProgrammeStages { get; set; }
        public DbSet<CourseType> CourseTypes { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<ProgrammeStageCourse> ProgrammeStageCourses { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<StudentGroup> StudentGroups { get; set; }
        public DbSet<Coordinator> Coordinators { get; set; }
        public DbSet<CourseEdition> CourseEditions { get; set; }
        public DbSet<CoordinatorCourseEdition> CoordinatorCourseEditions { get; set; }
        public DbSet<GroupCourseEdition> GroupCourseEditions { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<CourseRoom> CourseRooms { get; set; }
        public DbSet<Timestamp> Timestamps { get; set; }
        public DbSet<CourseRoomTimestamp> CourseRoomTimestamps { get; set; }
        public DbSet<SchedulePosition> SchedulePositions { get; set; }
        public DbSet<ScheduledMove> ScheduledMoves { get; set; }

        public ScheduleDesignerDbContext(DbContextOptions<ScheduleDesignerDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //Staffs
            //TEST ONLY
            modelBuilder.Entity<Staff>()
                .HasData(new Staff
                {
                    StaffId = 34527, 
                    FirstName = "Damian", 
                    LastName = "Ślusarczyk", 
                    IsAdmin = true
                });

            //Programme
            modelBuilder.Entity<Programme>()
                .HasIndex(e => e.Name)
                .IsUnique(true);

            //ProgrammeStage
            modelBuilder.Entity<ProgrammeStage>()
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId });

            modelBuilder.Entity<ProgrammeStage>()
                .Property(e => e.ProgrammeStageId)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();
            
            //Course
            modelBuilder.Entity<Course>()
                .HasKey(e => new { e.CourseId });

            modelBuilder.Entity<Course>()
                .Property(e => e.CourseId)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();
            
            //ProgrammeStageCourse
            modelBuilder.Entity<ProgrammeStageCourse>()
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.CourseId });

            modelBuilder.Entity<ProgrammeStageCourse>()
                .HasOne(e => e.ProgrammeStage)
                .WithMany(e => e.ProgrammeStageCourses)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<ProgrammeStageCourse>()
                .HasOne(e => e.Course)
                .WithMany(e => e.ProgrammeStageCourses)
                .OnDelete(DeleteBehavior.Restrict);

            //Class
            modelBuilder.Entity<Class>()
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.ClassId });

            modelBuilder.Entity<Class>()
                .Property(e => e.ClassId)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            //Group
            modelBuilder.Entity<Group>()
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.ClassId, e.GroupId });

            modelBuilder.Entity<Group>()
                .Property(e => e.GroupId)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            //Student
            modelBuilder.Entity<Student>()
                .HasKey(e => e.StudentId);

            modelBuilder.Entity<Student>()
                .Property(e => e.StudentId)
                .ValueGeneratedNever();

            //StudentGroup
            modelBuilder.Entity<StudentGroup>()
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.ClassId, e.GroupId, e.StudentId });

            //Coordinator
            modelBuilder.Entity<Coordinator>()
                .HasKey(e => e.CoordinatorId);

            modelBuilder.Entity<Coordinator>()
                .Property(e => e.CoordinatorId)
                .ValueGeneratedNever();

            //CourseEdition
            modelBuilder.Entity<CourseEdition>()
                .HasKey(e => new { e.CourseId, e.CourseEditionId });

            modelBuilder.Entity<CourseEdition>()
                .Property(e => e.CourseEditionId)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn();

            //CoordinatorCourseEdition
            modelBuilder.Entity<CoordinatorCourseEdition>()
                .HasKey(e => new { e.CourseId, e.CourseEditionId, e.CoordinatorId });

            //GroupCourseEdition
            modelBuilder.Entity<GroupCourseEdition>()
                .HasKey(e => new { e.ProgrammeId, e.ProgrammeStageId, e.CourseId, e.CourseEditionId, e.ClassId, e.GroupId });

            modelBuilder.Entity<GroupCourseEdition>()
                .HasOne(e => e.Group)
                .WithMany(e => e.CourseEditions)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GroupCourseEdition>()
                .HasOne(e => e.CourseEdition)
                .WithMany(e => e.Groups)
                .OnDelete(DeleteBehavior.Restrict);

            //CourseRoom
            modelBuilder.Entity<CourseRoom>()
                .HasKey(e => new { e.CourseId, e.RoomId });

            //Timestamp
            modelBuilder.Entity<Timestamp>()
                .HasIndex(p => new {SlotIndex = p.PeriodIndex, p.Day, p.Week })
                .IsUnique(true);

            //CourseRoomTimestamp
            modelBuilder.Entity<CourseRoomTimestamp>()
                .HasKey(e => new { e.RoomId, e.TimestampId, e.CourseId });

            modelBuilder.Entity<CourseRoomTimestamp>()
                .HasOne(e => e.SchedulePosition)
                .WithOne(e => e.CourseRoomTimestamp)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CourseRoomTimestamp>()
                .HasMany(e => e.ScheduledMovesDestinations)
                .WithOne(e => e.Destination)
                .OnDelete(DeleteBehavior.Restrict);

            //SchedulePosition
            modelBuilder.Entity<SchedulePosition>()
                .HasKey(e => new { e.RoomId, e.TimestampId, e.CourseId });

            modelBuilder.Entity<SchedulePosition>()
                .HasMany(e => e.ScheduledMoves)
                .WithOne(e => e.Origin)
                .OnDelete(DeleteBehavior.Restrict);

            //ScheduledMoves
            modelBuilder.Entity<ScheduledMove>()
                .HasKey(e => new { e.RoomId_1, e.TimestampId_1, e.RoomId_2, e.TimestampId_2, e.CourseId });

            //Staff
            modelBuilder.Entity<Staff>()
                .HasKey(e => e.StaffId);

            modelBuilder.Entity<Staff>()
                .Property(e => e.StaffId)
                .ValueGeneratedNever();


            base.OnModelCreating(modelBuilder);
        }
    }
}
