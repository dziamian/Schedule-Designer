using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ScheduleDesigner
{
    public static class Helpers
    {
        public class DatabaseConnectionOptions
        {
            public string SchedulingDatabase { get; set; }
        }

        public class ApplicationOptions
        {
            public string BaseUsosUrl { get; set; }
        }

        public class Consumer
        {
            public string Key { get; set; }

            public string Secret { get; set; }
        }

        public class FullBackup
        {
            public int StartHour { get; set; }
            public int IntervalHours { get; set; }
            public string Path { get; set; }
        }

        public class DifferentialBackup
        {
            public int StartHour { get; set; }
            public int IntervalHours { get; set; }
            public string Path { get; set; }
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

        public class CoordinatorPositionKey : IComparable<CoordinatorPositionKey>
        {
            public int CoordinatorId { get; set; }

            public int TimestampId { get; set; }

            public int CompareTo(CoordinatorPositionKey other)
            {
                int result = this.CoordinatorId.CompareTo(other.CoordinatorId);
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

        public class GroupPositionKey : IComparable<GroupPositionKey>
        {
            public int GroupId { get; set; }

            public int TimestampId { get; set; }


            public int CompareTo(GroupPositionKey other)
            {
                int result = this.GroupId.CompareTo(other.GroupId);
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

        public class ScheduleAmount
        {
            public int CourseEditionId { get; set; }
            public int Count { get; set; }
        }

        public static List<int> GetNestedGroupsIds(CourseEdition courseEdition, IGroupRepo _groupRepo)
        {
            var groups = courseEdition.Groups.Select(e => e.Group).ToList();
            var groupsIds = groups.Select(e => e.GroupId).ToList();

            var startIndex = 0;
            var endIndex = groups.Count;
            var oldSize = endIndex;
            while (groups.GetRange(startIndex, endIndex - startIndex).Any(e => e.ParentGroupId != null))
            {
                var _parentGroups = _groupRepo
                    .Get(e => groupsIds.GetRange(startIndex, endIndex - startIndex).Contains(e.GroupId) && e.ParentGroup != null)
                    .Include(e => e.ParentGroup)
                    .Select(e => e.ParentGroup);

                groups.AddRange(_parentGroups);
                groupsIds.AddRange(_parentGroups.Select(e => e.GroupId).ToList());

                startIndex = endIndex;
                endIndex = groups.Count;
            }

            var _childGroups = _groupRepo
                .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(0, oldSize).Contains((int)e.ParentGroupId));

            while (_childGroups.Any())
            {
                groupsIds.AddRange(_childGroups.Select(e => e.GroupId).ToList());

                startIndex = endIndex;
                endIndex += _childGroups.Count();

                _childGroups = _groupRepo
                    .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(startIndex, endIndex - startIndex).Contains((int)e.ParentGroupId));
            }

            return groupsIds;
        }
    }
}
