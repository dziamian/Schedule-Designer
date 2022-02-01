﻿using Microsoft.EntityFrameworkCore;
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
    public class ScheduleDesignerDbContext : DbContext
    {
        public DbSet<Authorization> Authorizations { get; set; }
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
        public DbSet<SchedulePosition> SchedulePositions { get; set; }
        public DbSet<ScheduledMovePosition> ScheduledMovePositions { get; set; }
        public DbSet<ScheduledMove> ScheduledMoves { get; set; }
        public DbSet<Message> Messages { get; set; }

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

            modelBuilder.Entity<CoordinatorCourseEdition>()
                .HasOne(e => e.Coordinator)
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
