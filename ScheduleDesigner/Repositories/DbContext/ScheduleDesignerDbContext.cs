﻿using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories
{
    public class ScheduleDesignerDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Coordinator> Coordinators { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<Settings> Settings { get; set; }
        public DbSet<CourseType> CourseTypes { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<StudentGroup> StudentGroups { get; set; }
        public DbSet<CourseEdition> CourseEditions { get; set; }
        public DbSet<CoordinatorCourseEdition> CoordinatorCourseEditions { get; set; }
        public DbSet<GroupCourseEdition> GroupCourseEditions { get; set; }
        public DbSet<RoomType> RoomTypes { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<CourseRoom> CourseRooms { get; set; }
        public DbSet<Timestamp> Timestamps { get; set; }
        public DbSet<CourseRoomTimestamp> CourseRoomTimestamps { get; set; }
        public DbSet<SchedulePosition> SchedulePositions { get; set; }
        public DbSet<ScheduledMove> ScheduledMoves { get; set; }

        public ScheduleDesignerDbContext(DbContextOptions<ScheduleDesignerDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //User
            modelBuilder.Entity<User>()
                .HasKey(e => e.UserId);

            modelBuilder.Entity<User>()
                .Property(e => e.UserId)
                .ValueGeneratedNever();

            //Student
            modelBuilder.Entity<Student>()
                .HasKey(e => e.UserId);

            modelBuilder.Entity<Student>()
                .Property(e => e.UserId)
                .ValueGeneratedNever();

            //Coordinator
            modelBuilder.Entity<Coordinator>()
                .HasKey(e => e.UserId);

            modelBuilder.Entity<Coordinator>()
                .Property(e => e.UserId)
                .ValueGeneratedNever();

            //Staff
            modelBuilder.Entity<Staff>()
                .HasKey(e => e.UserId);

            modelBuilder.Entity<Staff>()
                .Property(e => e.UserId)
                .ValueGeneratedNever();

            //TEST ONLY
            modelBuilder.Entity<User>()
                .HasData(new User
                    {
                        UserId = 34527,
                        FirstName = "Damian",
                        LastName = "Ślusarczyk"
                    }
                );
            modelBuilder.Entity<Student>()
                .HasData(new Student
                    {
                        UserId = 34527
                    }
                );
            modelBuilder.Entity<Staff>()
                .HasData(new Staff
                    {
                        UserId = 34527,
                        IsAdmin = true
                    }
                );

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

            //CoordinatorCourseEdition
            modelBuilder.Entity<CoordinatorCourseEdition>()
                .HasKey(e => new { e.CourseId, e.CourseEditionId, e.CoordinatorId });

            //GroupCourseEdition
            modelBuilder.Entity<GroupCourseEdition>()
                .HasKey(e => new { e.CourseId, e.CourseEditionId, e.GroupId });

            //CourseRoom
            modelBuilder.Entity<CourseRoom>()
                .HasKey(e => new { e.CourseId, e.RoomId });

            //Timestamp
            modelBuilder.Entity<Timestamp>()
                .HasIndex(p => new { SlotIndex = p.PeriodIndex, p.Day, p.Week })
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

            //ScheduledMove
            modelBuilder.Entity<ScheduledMove>()
                .HasKey(e => new { e.MoveId, e.RoomId_1, e.TimestampId_1, e.RoomId_2, e.TimestampId_2, e.CourseId });


            base.OnModelCreating(modelBuilder);
        }
    }
}