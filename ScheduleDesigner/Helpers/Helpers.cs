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
        /// <summary>
        /// Funkcja sprawdzająca poprawność liczby minut do odbycia w semestrze (czy istnieje pełna liczba jednostek zajęciowych do odbycia).
        /// </summary>
        /// <param name="unitsMinutes">Liczba minut do odbycia w semestrze</param>
        /// <param name="settings">Ustawienia aplikacji</param>
        /// <returns>Prawdę jeśli liczba minut jest poprawna w stosunku do ustawień aplikacji, w przeciwnym wypadku fałsz</returns>
        public static bool AreUnitsMinutesValid(int unitsMinutes, Settings settings)
        {
            return (unitsMinutes % settings.CourseDurationMinutes == 0) || (unitsMinutes * 2 / settings.CourseDurationMinutes % settings.TermDurationWeeks == 0);
        }

        /// <summary>
        /// Metoda tworząca ramy czasowe dla podanych ustawień aplikacji i dodająca je do bazy danych.
        /// </summary>
        /// <param name="settings">Ustawienia aplikacji</param>
        /// <param name="connectionString">Wyrażenie wymagane do połączenia się z bazą danych</param>
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

        /// <summary>
        /// Metoda usuwająca wszystkie ramy czasowe z bazy danych.
        /// </summary>
        /// <param name="unitOfWork">Instancja klasy wzorca UoW.</param>
        public static void RemoveTimestamps(IUnitOfWork unitOfWork)
        {
            unitOfWork.Context.Database.ExecuteSqlRaw("DELETE FROM [Timestamps]");
        }

        /// <summary>
        /// Funkcja zwracająca informacje o powiązaniach grup dostępnych w systemie.
        /// </summary>
        /// <param name="_groupRepo">Instancja wzorca repozytorium dla modelu <see cref="Group"/></param>
        /// <returns>Kolekcję powiązań grup</returns>
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

        /// <summary>
        /// Funkcja zwracająca informacje o powiązanych grupach z edycjami zajęć.
        /// </summary>
        /// <param name="_groupCourseEditionRepo">Instancja wzorca repozytorium dla modelu <see cref="GroupCourseEdition"/></param>
        /// <returns>Kolekcję powiązań grup z edycjami zajęć</returns>
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

        /// <summary>
        /// Funkcja zwracająca informacje o powiązanych (użytkowników) prowadzących z edycjami zajęć.
        /// </summary>
        /// <param name="_coordinatorCourseEditionRepo">Instancja wzorca repozytorium dla modelu <see cref="CoordinatorCourseEdition"/></param>
        /// <returns>Kolekcję powiązań prowadzących (użytkowników) z edycjami zajęć</returns>
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

        /// <summary>
        /// Funkcja zwracająca powiązania identyfikatorów przedmiotów z ich maksymalną liczbą jednostek zajęciowych możliwych do zrealizowania w ciągu semestru.
        /// </summary>
        /// <param name="_courseRepo">Instancja wzorca repozytorium dla modelu <see cref="Course"/></param>
        /// <param name="courseDurationMinutes">Liczba minut pojedynczej jednostki zajęciowej</param>
        /// <returns>Kolekcję powiązań identyfikatorów przedmiotów z ich maksymalną liczbą jednostek zajęciowych</returns>
        public static Dictionary<int, int> GetMaxCourseUnits(ICourseRepo _courseRepo, int courseDurationMinutes)
        {
            var maxCourseUnits = new Dictionary<int, int>();

            _courseRepo.GetAll().ToList().ForEach(course =>
            {
                maxCourseUnits.Add(course.CourseId, (int) Math.Ceiling(course.UnitsMinutes / (courseDurationMinutes * 1.0)));
            });

            return maxCourseUnits;
        }

        /// <summary>
        /// Zwraca identyfikatory grup nadrzędnych do grup podanych w liście.
        /// </summary>
        /// <param name="groups">Lista grup, dla których należy odnaleźć grupy nadrzędne</param>
        /// <param name="_groupRepo">Instancja wzorca repozytorium dla modelu <see cref="Group"/></param>
        /// <returns>Listę identyfikatorów grup nadrzędnych wraz z tymi podanymi w parametrze</returns>
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

        /// <summary>
        /// Zwraca potrójną wartość dotyczącą wybranej grupy - listę grup nadrzędnych, pełną nazwę grupy oraz poziom węzła.
        /// </summary>
        /// <param name="_group">Kolekcja wspierająca ładowanie danych i posiadająca grupę z załadowaną grupą nadrzędną</param>
        /// <param name="group">Grupa będąca w kolekcji</param>
        /// <returns>Potrójną wartość - listę grup nadrzędnych, pełną nazwę grupy oraz poziom węzła</returns>
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

        /// <summary>
        /// Zwraca identyfikatory grup podrzędnych do grup podanych w liście.
        /// </summary>
        /// <param name="groups">Lista grup, dla których należy odnaleźć grupy podrzędne</param>
        /// <param name="_groupRepo">Instancja wzorca repozytorium dla modelu <see cref="Group"/></param>
        /// <returns>Listę identyfikatorów grup podrzędnych wraz z tymi podanymi w parametrze</returns>
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

        /// <summary>
        /// Zwraca identyfikatory grup nadrzędnych i podrzędnych dla grup, które są przypisane do wybranej edycji zajęć.
        /// </summary>
        /// <param name="courseEdition">Wybrana edycja zajęć</param>
        /// <param name="_groupRepo">Instancja wzorca repozytorium dla modelu <see cref="Group"/></param>
        /// <returns>Listę identyfikatorów grup nadrzędnych i podrzędnych</returns>
        public static List<int> GetNestedGroupsIds(CourseEdition courseEdition, IGroupRepo _groupRepo)
        {
            return GetNestedGroupsIds(courseEdition.Groups.Select(e => e.Group).ToList(), _groupRepo);
        }

        /// <summary>
        /// Zwraca identyfikatory grup nadrzędnych i podrzędnych do grup podanych w liście.
        /// </summary>
        /// <param name="groups">Lista grup, dla których należy odnaleźć grupy nadrzędne i podrzędne</param>
        /// <param name="_groupRepo">Instancja wzorca repozytorium dla modelu <see cref="Group"/></param>
        /// <returns>Listę identyfikatorów grup nadrzędnych i podrzędnych wraz z tymi podanymi w parametrze</returns>
        public static List<int> GetNestedGroupsIds(List<Group> groups, IGroupRepo _groupRepo)
        {
            var parentGroups = GetParentGroups(new List<Group>(groups), _groupRepo);
            var childGroups = GetChildGroups(new List<Group>(groups), _groupRepo);

            return parentGroups.Union(childGroups).ToList();
        }
    }

    /// <summary>
    /// Klasa reprezentująca informację o połączeniu z bazą danych.
    /// </summary>
    public class DatabaseConnectionOptions
    {
        /// <summary>
        /// Wyrażenie wymagane do połączenia się z bazą danych.
        /// </summary>
        public string SchedulingDatabase { get; set; }
    }

    /// <summary>
    /// Klasa reprezentująca informacje o opcjach aplikacji.
    /// </summary>
    public class ApplicationOptions
    {
        /// <summary>
        /// Bazowy adres instalacji systemu USOS.
        /// </summary>
        public string BaseUsosUrl { get; set; }
    }

    /// <summary>
    /// Klasa reprezentująca informację o kluczu projektu wygenerowanego dla USOS API.
    /// </summary>
    public class Consumer
    {
        /// <summary>
        /// Klucz projektu.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Sekret klucza projektu.
        /// </summary>
        public string Secret { get; set; }
    }

    /// <summary>
    /// Klasa reprezentująca informacje dotyczące wykonywania pełnych kopii zapasowych.
    /// </summary>
    public class FullBackup
    {
        /// <summary>
        /// Godzina wykonania pierwszej kopii zapasowej.
        /// </summary>
        public int StartHour { get; set; }
        
        /// <summary>
        /// Przedział czasowy (liczba godzin) co ile ma się wykonywać kopia zapasowa, zaczynając od godziny startowej.
        /// </summary>
        public int IntervalHours { get; set; }

        /// <summary>
        /// Ścieżka zapisu utworzonej kopii zapasowej.
        /// </summary>
        public string Path { get; set; }
    }

    /// <summary>
    /// Klasa reprezentująca informacje dotyczące wykonywania różnicowych kopii zapasowych.
    /// </summary>
    public class DifferentialBackup
    {
        /// <summary>
        /// Godzina wykonania pierwszej kopii zapasowej.
        /// </summary>
        public int StartHour { get; set; }

        /// <summary>
        /// Przedział czasowy (liczba godzin) co ile ma się wykonywać kopia zapasowa, zaczynając od godziny startowej.
        /// </summary>
        public int IntervalHours { get; set; }

        /// <summary>
        /// Ścieżka zapisu utworzonej kopii zapasowej.
        /// </summary>
        public string Path { get; set; }
    }

    /// <summary>
    /// Klasa reprezentująca identyfikator zasobu serwera dotyczącego edycji zajęć.
    /// </summary>
    public class CourseEditionKey : IComparable<CourseEditionKey>
    {
        /// <summary>
        /// Identyfikator przedmiotu.
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Identyfikator edycji zajęć.
        /// </summary>
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

    /// <summary>
    /// Klasa reprezentująca identyfikator zasobu serwera dotyczącego pozycji w planie.
    /// </summary>
    public class SchedulePositionKey : IComparable<SchedulePositionKey>
    {
        /// <summary>
        /// Identyfikator pokoju.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Identyfikator ramy czasowej na planie.
        /// </summary>
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

    /// <summary>
    /// Klasa reprezentująca identyfikator zasobu serwera dotyczącego pozycji w planie (z perspektywy prowadzących).
    /// </summary>
    public class CoordinatorPositionKey : IComparable<CoordinatorPositionKey>
    {
        /// <summary>
        /// Identyfikator prowadzącego (użytkownika).
        /// </summary>
        public int CoordinatorId { get; set; }

        /// <summary>
        /// Identyfikator ramy czasowej na planie.
        /// </summary>
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

    /// <summary>
    /// Klasa reprezentująca identyfikator zasobu serwera dotyczącego pozycji w planie (z perspektywy grup).
    /// </summary>
    public class GroupPositionKey : IComparable<GroupPositionKey>
    {
        /// <summary>
        /// Identyfikator grupy zajęciowej.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Identyfikator ramy czasowej na planie.
        /// </summary>
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

    /// <summary>
    /// Klasa reprezentująca powiązania między grupami w systemie.
    /// </summary>
    public class GroupIds
    {
        /// <summary>
        /// Lista identyfikatorów grup nadrzędnych.
        /// </summary>
        public List<int> Parents { get; set; }

        /// <summary>
        /// Lista identyfikatorów grup podrzędnych.
        /// </summary>
        public List<int> Childs { get; set; }
    }

    /// <summary>
    /// Interfejs pozwalający konwertowanie modelu danych na wiersz w pliku CSV.
    /// </summary>
    public interface IExportCsv
    {
        /// <summary>
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        string GetHeader(string delimiter = "|");

        /// <summary>
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        string GetRow(string delimiter = "|");
    }
}
