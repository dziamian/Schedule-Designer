using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ScheduleDesigner.Dtos
{
    public class SchedulePositionDto
    {
        public int RoomId { get; set; }
        public int TimestampId { get; set; }
        public int CourseId { get; set; }
        public int CourseEditionId { get; set; }
    }

    public class CourseEditionDto
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
    }

    public class CoordinatorCourseEditionDto
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        public int CoordinatorId { get; set; }
    }

    public class GroupCourseEditionDto
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        public int GroupId { get; set; }
    }

    public class GroupDto
    {
        public int GroupId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public int? ParentGroupId { get; set; }
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
    }

    public class GroupFullName
    {
        public string FullName { get; set; }
        public ICollection<int> GroupsIds { get; set; }
        public int Levels { get; set; }
    }

    public class RoomName
    {
        public string Name { get; set; }
    }

    public class RoomAvailability
    {
        public int RoomId { get; set; }
        public bool IsBusy { get; set; }
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
