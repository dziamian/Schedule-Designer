using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ScheduleDesigner.Dtos
{
    public class TimestampDto
    {
        public int TimestampId { get; set; }
        public int PeriodIndex { get; set; }
        [Range(1, 5, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Day { get; set; }
        public int Week { get; set; }

        public Timestamp FromDto()
        {
            return new Timestamp
            {
                TimestampId = TimestampId,
                PeriodIndex = PeriodIndex,
                Day = Day,
                Week = Week
            };
        }
    }

    public class SchedulePositionDto
    {
        public int RoomId { get; set; }
        public int TimestampId { get; set; }
        public int CourseId { get; set; }
        public int CourseEditionId { get; set; }

        public SchedulePosition FromDto()
        {
            return new SchedulePosition
            {
                RoomId = RoomId,
                TimestampId = TimestampId,
                CourseId = CourseId,
                CourseEditionId = CourseEditionId
            };
        }
    }

    public class CourseEditionDto
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        public CourseEdition FromDto()
        {
            return new CourseEdition
            {
                CourseId = CourseId,
                CourseEditionId = CourseEditionId,
                Name = Name,
            };
        }
    }

    public class CoordinatorCourseEditionDto
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        public int CoordinatorId { get; set; }

        public CoordinatorCourseEdition FromDto()
        {
            return new CoordinatorCourseEdition
            {
                CourseId = CourseId,
                CourseEditionId = CourseEditionId,
                CoordinatorId = CoordinatorId
            };
        }
    }

    public class GroupCourseEditionDto
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        public int GroupId { get; set; }

        public GroupCourseEdition FromDto()
        {
            return new GroupCourseEdition
            {
                CourseId = CourseId,
                CourseEditionId = CourseEditionId,
                GroupId = GroupId
            };
        }
    }

    public class GroupDto
    {
        public int GroupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public int? ParentGroupId { get; set; }

        public Group FromDto()
        {
            return new Group
            {
                GroupId = GroupId,
                Name = Name,
                ParentGroupId = ParentGroupId
            };
        }
    }

    public class CourseDto
    {
        public int CourseId { get; set; }

        public int CourseTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int UnitsMinutes { get; set; }

        public Course FromDto()
        {
            return new Course
            {
                CourseId = CourseId,
                CourseTypeId = CourseTypeId,
                Name = Name,
                UnitsMinutes = UnitsMinutes
            };
        }
    }

    public class CourseRoomDto
    {
        public int CourseId { get; set; }

        public int RoomId { get; set; }

        public int? UserId { get; set; }

        public CourseRoom FromDto()
        {
            return new CourseRoom
            {
                CourseId = CourseId,
                RoomId = RoomId,
                UserId = UserId
            };
        }
    }

    public class CourseTypeDto
    {
        public int CourseTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(9)]
        [RegularExpression(@"^#(?:[0-9a-fA-F]{3,4}){1,2}$")]
        public string Color { get; set; }

        public CourseType FromDto()
        {
            return new CourseType
            {
                CourseTypeId = CourseTypeId,
                Name = Name,
                Color = Color
            };
        }
    }

    public class RoomTypeDto
    {
        public int RoomTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public RoomType FromDto()
        {
            return new RoomType
            {
                RoomTypeId = RoomTypeId,
                Name = Name
            };
        }
    }

    public class RoomDto
    {
        public int RoomId { get; set; }

        public int RoomTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Capacity { get; set; }

        public Room FromDto()
        {
            return new Room
            {
                RoomId = RoomId,
                RoomTypeId = RoomTypeId,
                Name = Name,
                Capacity = Capacity
            };
        }
    }

    public class StudentGroupDto
    {
        public int GroupId { get; set; }

        public int StudentId { get; set; }

        public bool IsRepresentative { get; set; }

        public StudentGroup FromDto()
        {
            return new StudentGroup
            {
                GroupId = GroupId,
                StudentId = StudentId,
                IsRepresentative = IsRepresentative
            };
        }
    }

    public class GroupFullName
    {
        public string BasicName { get; set; }
        public string FullName { get; set; }
        public ICollection<int> GroupsIds { get; set; }
        public int Levels { get; set; }
    }

    public class GroupFullInfo
    {
        public int GroupId { get; set; }
        public string BasicName { get; set; }
        public string FullName { get; set; }
        public ICollection<int> ParentIds { get; set; }
        public ICollection<int> ChildIds { get; set; }
    }

    public class RoomName
    {
        public string Name { get; set; }
    }

    public class RoomAvailability
    {
        public int RoomId { get; set; }
        public bool IsBusy { get; set; }

        public override bool Equals(object obj)
        {
            return obj is RoomAvailability availability &&
                   RoomId == availability.RoomId &&
                   IsBusy == availability.IsBusy;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RoomId, IsBusy);
        }
    }

    public class ScheduledMoveRead : IComparable<ScheduledMoveRead>
    {
        public int MoveId { get; set; }

        public bool IsConfirmed { get; set; }

        public int UserId { get; set; }

        public IEnumerable<int> SourceWeeks { get; set; }

        public int DestRoomId { get; set; }

        public string DestRoomName { get; set; }

        public int DestRoomTypeId { get; set; }

        public int DestPeriodIndex { get; set; }

        public int DestDay { get; set; }

        public IEnumerable<int> DestWeeks { get; set; }

        public DateTime ScheduleOrder { get; set; }

        public int CompareTo(ScheduledMoveRead other)
        {
            int result = this.ScheduleOrder.CompareTo(other.ScheduleOrder);
            if (result != 0)
            {
                return result;
            }
            return 0;
        }
    }
}
