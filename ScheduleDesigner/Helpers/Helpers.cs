using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using ScheduleDesigner.Dtos;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories.UnitOfWork;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace ScheduleDesigner.Helpers
{
    public static class Methods
    {
        public static bool AreUnitsMinutesValid(int unitsMinutes, Settings settings)
        {
            return (unitsMinutes % settings.CourseDurationMinutes == 0) || (unitsMinutes * 2 / settings.CourseDurationMinutes % settings.TermDurationWeeks == 0);
        }

        public static void AddTimestamps(Settings settings, string connectionString)
        {
            var timestamps = new List<TimestampDto>();

            var numberOfSlots = (settings.EndTime - settings.StartTime).TotalMinutes / settings.CourseDurationMinutes;
            var numberOfWeeks = settings.TermDurationWeeks;

            var id = 1;
            for (int k = 0; k < numberOfWeeks; ++k)
            {
                for (int j = 0; j < 5; ++j)
                {
                    for (int i = 0; i < numberOfSlots; ++i)
                    {
                        timestamps.Add(new TimestampDto { TimestampId = id++, PeriodIndex = i + 1, Day = j + 1, Week = k + 1 });
                    }
                }
            }

            BulkImport<TimestampDto>.Execute(connectionString, "dbo.Timestamps", timestamps);
        }

        public static void RemoveTimestamps(IUnitOfWork unitOfWork)
        {
            unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [Timestamps]");
        }

        public static Dictionary<int, GroupIds> GetGroups(IGroupRepo _groupRepo)
        {
            var groupsDictionary = new Dictionary<int, GroupIds>();

            _groupRepo.GetAll().ToList().ForEach(group =>
            {
                bool found = groupsDictionary.TryGetValue(group.GroupId, out var groupIds);
                if (!found)
                {
                    groupIds = new GroupIds
                    {
                        Parents = new List<int>(),
                        Childs = new List<int>()
                    };
                }

                if (group.ParentGroupId != null)
                {
                    var parentGroupId = (int) group.ParentGroupId;
                    if (!groupsDictionary.TryGetValue(parentGroupId, out var parent))
                    {
                        parent = new GroupIds
                        {
                            Parents = new List<int>(),
                            Childs = new List<int>()
                        };
                        groupsDictionary.Add(parentGroupId, parent);
                    } 
                    groupIds.Parents.Add(parentGroupId);
                    groupIds.Parents.AddRange(parent.Parents);
                    parent.Childs.Add(group.GroupId);
                    parent.Childs.AddRange(groupIds.Childs);

                    foreach (var g in parent.Parents)
                    {
                        groupsDictionary.TryGetValue(g, out var p);
                        p.Childs.Add(group.GroupId);
                    }

                    foreach (var g in groupIds.Childs)
                    {
                        groupsDictionary.TryGetValue(g, out var c);
                        c.Parents.AddRange(groupIds.Parents);
                    }
                }
                if (!found)
                {
                    groupsDictionary.Add(group.GroupId, groupIds);
                }
            });

            return groupsDictionary;
        }

        public static Dictionary<CourseEditionKey, List<int>> GetGroupCourseEditions(IGroupCourseEditionRepo _groupCourseEditionRepo)
        {
            var groupCourseEditions = new Dictionary<CourseEditionKey, List<int>>();

            _groupCourseEditionRepo.GetAll().ToList().ForEach(groupCourseEdition =>
            {
                var courseEditionKey = new CourseEditionKey
                {
                    CourseId = groupCourseEdition.CourseId,
                    CourseEditionId = groupCourseEdition.CourseEditionId
                };

                if (groupCourseEditions.TryGetValue(courseEditionKey, out var groups))
                {
                    groups.Add(groupCourseEdition.GroupId);
                } 
                else
                {
                    groupCourseEditions.Add(courseEditionKey, new List<int>() { groupCourseEdition.GroupId });
                }

            });

            return groupCourseEditions;
        }

        public static Dictionary<CourseEditionKey, List<int>> GetCoordinatorCourseEditions(ICoordinatorCourseEditionRepo _coordinatorCourseEditionRepo)
        {
            var coordinatorCourseEditions = new Dictionary<CourseEditionKey, List<int>>();

            _coordinatorCourseEditionRepo.GetAll().ToList().ForEach(coordinatorCourseEdition =>
            {
                var courseEditionKey = new CourseEditionKey
                {
                    CourseId = coordinatorCourseEdition.CourseId,
                    CourseEditionId = coordinatorCourseEdition.CourseEditionId
                };

                if (coordinatorCourseEditions.TryGetValue(courseEditionKey, out var coordinators))
                {
                    coordinators.Add(coordinatorCourseEdition.CoordinatorId);
                }
                else
                {
                    coordinatorCourseEditions.Add(courseEditionKey, new List<int>() { coordinatorCourseEdition.CoordinatorId });
                }

            });

            return coordinatorCourseEditions;
        }

        public static Dictionary<int, int> GetMaxCourseUnits(ICourseRepo _courseRepo, int courseDurationMinutes)
        {
            var maxCourseUnits = new Dictionary<int, int>();

            _courseRepo.GetAll().ToList().ForEach(course =>
            {
                maxCourseUnits.Add(course.CourseId, (int) Math.Ceiling(course.UnitsMinutes / (courseDurationMinutes * 1.0)));
            });

            return maxCourseUnits;
        }

        public static List<int> GetParentGroups(List<Group> groups, IGroupRepo _groupRepo)
        {
            var groupsIds = groups.Select(e => e.GroupId).ToList();

            var startIndex = 0;
            var endIndex = groups.Count;
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

            return groupsIds;
        }

        public static Tuple<List<int>, string, int> GetParentGroupsWithFullNameAndLevels(
            IIncludableQueryable<Group, Group> _group, Group group)
        {
            var fullGroupName = group.Name;
            var groupIds = new List<int>() { group.GroupId };
            var levels = 0;

            while (group.ParentGroupId != null)
            {
                ++levels;
                _group = _group.ThenInclude(e => e.ParentGroup);
                group = _group.First();
                for (var i = 0; i < levels; ++i)
                {
                    group = group.ParentGroup;
                }
                fullGroupName = group.Name + fullGroupName;
                groupIds.Add(group.GroupId);
            }

            return new Tuple<List<int>, string, int>(groupIds, fullGroupName, levels);
        }

        public static List<int> GetChildGroups(List<Group> groups, IGroupRepo _groupRepo)
        {
            var groupsIds = groups.Select(e => e.GroupId).ToList();

            var startIndex = 0;
            var endIndex = groups.Count;

            var _childGroups = _groupRepo
                .Get(e => (e.ParentGroupId != null) && groupsIds.Contains((int)e.ParentGroupId))
                .ToList();
            while (_childGroups.Any())
            {
                groupsIds.AddRange(_childGroups.Select(e => e.GroupId).ToList());

                startIndex = endIndex;
                endIndex += _childGroups.Count();

                _childGroups = _groupRepo
                    .Get(e => (e.ParentGroupId != null) && groupsIds.GetRange(startIndex, endIndex - startIndex).Contains((int)e.ParentGroupId))
                    .ToList();
            }
            
            return groupsIds;
        }

        public static List<int> GetNestedGroupsIds(CourseEdition courseEdition, IGroupRepo _groupRepo)
        {
            return GetNestedGroupsIds(courseEdition.Groups.Select(e => e.Group).ToList(), _groupRepo);
        }

        public static List<int> GetNestedGroupsIds(List<Group> groups, IGroupRepo _groupRepo)
        {
            var parentGroups = GetParentGroups(new List<Group>(groups), _groupRepo);
            var childGroups = GetChildGroups(new List<Group>(groups), _groupRepo);

            return parentGroups.Union(childGroups).ToList();
        }
    }

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

    public class GroupIds
    {
        public List<int> Parents { get; set; }

        public List<int> Childs { get; set; }
    }

    public interface IExportCsv
    {
        string GetHeader(string delimiter = "|");
        string GetRow(string delimiter = "|");
    }
}
