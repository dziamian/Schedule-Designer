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

    public class SchedulePositionKey : IComparable<SchedulePositionKey>
    {
        public int RoomId { get; set; }

        public int PeriodIndex { get; set; }

        public int Day { get; set; }

        public int Week { get; set; }

        public int CompareTo(SchedulePositionKey other)
        {
            int result = this.RoomId.CompareTo(other.RoomId);
            if (result != 0)
            {
                return result;
            }
            result = this.PeriodIndex.CompareTo(other.PeriodIndex);
            if (result != 0)
            {
                return result;
            }
            result = this.Day.CompareTo(other.Day);
            if (result != 0)
            {
                return result;
            }
            result = this.Week.CompareTo(other.Week);
            if (result != 0)
            {
                return result;
            }
            return 0;
        }

        public bool Equals(SchedulePositionKey key)
        {
            return key.RoomId.Equals(RoomId) && key.PeriodIndex.Equals(PeriodIndex)
                && key.Day.Equals(Day) && key.Week.Equals(Week);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as SchedulePositionKey);
        }

        public override int GetHashCode()
        {
            return RoomId.GetHashCode() ^ PeriodIndex.GetHashCode() ^ Day.GetHashCode() ^ Week.GetHashCode();
        }
    }

    public class CoordinatorPositionKey
    {
        public int CoordinatorId { get; set; }

        public int PeriodIndex { get; set; }

        public int Day { get; set; }

        public int Week { get; set; }


        public bool Equals(CoordinatorPositionKey key)
        {
            return key.CoordinatorId.Equals(CoordinatorId) && key.PeriodIndex.Equals(PeriodIndex)
                                                           && key.Day.Equals(Day) && key.Week.Equals(Week);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as CoordinatorPositionKey);
        }

        public override int GetHashCode()
        {
            return CoordinatorId.GetHashCode() ^ PeriodIndex.GetHashCode() ^ Day.GetHashCode() ^ Week.GetHashCode();
        }
    }

    public class GroupPositionKey
    {
        public int GroupId { get; set; }

        public int PeriodIndex { get; set; }

        public int Day { get; set; }

        public int Week { get; set; }


        public bool Equals(GroupPositionKey key)
        {
            return key.GroupId.Equals(GroupId) && key.PeriodIndex.Equals(PeriodIndex)
                                               && key.Day.Equals(Day) && key.Week.Equals(Week);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as GroupPositionKey);
        }

        public override int GetHashCode()
        {
            return GroupId.GetHashCode() ^ PeriodIndex.GetHashCode() ^ Day.GetHashCode() ^ Week.GetHashCode();
        }
    }
}
