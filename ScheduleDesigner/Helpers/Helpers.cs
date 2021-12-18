using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Helpers
{
    public class ApplicationInfo
    {
        public string BaseUsosUrl { get; set; }
    }

    public class Consumer
    {
        public string Key { get; set; }

        public string Secret { get; set; }
    }

    public class CourseEditionKey : IComparable<CourseEditionKey>
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        public int CompareTo(CourseEditionKey other)
        {
            int result = this.CourseId.CompareTo(other.CourseId);
            if (result != 0)
            {
                return result;
            }
            result = this.CourseEditionId.CompareTo(other.CourseEditionId);
            if (result != 0)
            {
                return result;
            }
            return 0;
        }

        public bool Equals(CourseEditionKey key)
        {
            return key.CourseId.Equals(CourseId) && key.CourseEditionId.Equals(CourseEditionId);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as CourseEditionKey);
        }

        public override int GetHashCode()
        {
            return CourseId.GetHashCode() ^ CourseEditionId.GetHashCode();
        }
    }

    public class ScheduledMoveRead : IComparable<ScheduledMoveRead>
    {
        public int MoveId { get; set; }

        public bool IsConfirmed { get; set; }

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

    public class SchedulePositionKey : IComparable<SchedulePositionKey>
    {
        public int RoomId { get; set; }

        public int TimestampId { get; set; }

        public int CompareTo(SchedulePositionKey other)
        {
            int result = this.RoomId.CompareTo(other.RoomId);
            if (result != 0)
            {
                return result;
            }
            result = this.TimestampId.CompareTo(other.TimestampId);
            if (result != 0)
            {
                return result;
            }
            return 0;
        }

        public bool Equals(SchedulePositionKey key)
        {
            return key.RoomId.Equals(RoomId)
                && key.TimestampId.Equals(TimestampId);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as SchedulePositionKey);
        }

        public override int GetHashCode()
        {
            return RoomId.GetHashCode() ^ TimestampId.GetHashCode();
        }
    }

    public class CoordinatorPositionKey
    {
        public int CoordinatorId { get; set; }

        public int TimestampId { get; set; }


        public bool Equals(CoordinatorPositionKey key)
        {
            return key.CoordinatorId.Equals(CoordinatorId)
                && key.TimestampId.Equals(TimestampId);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as CoordinatorPositionKey);
        }

        public override int GetHashCode()
        {
            return CoordinatorId.GetHashCode() ^ TimestampId.GetHashCode();
        }
    }

    public class GroupPositionKey
    {
        public int GroupId { get; set; }

        public int TimestampId { get; set; }


        public bool Equals(GroupPositionKey key)
        {
            return key.GroupId.Equals(GroupId) 
                && key.TimestampId.Equals(TimestampId);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as GroupPositionKey);
        }

        public override int GetHashCode()
        {
            return GroupId.GetHashCode() ^ TimestampId.GetHashCode();
        }
    }
}
