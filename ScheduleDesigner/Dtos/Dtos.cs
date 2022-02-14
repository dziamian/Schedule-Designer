using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ScheduleDesigner.Dtos
{
    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="Timestamp"/>.
    /// </summary>
    public class TimestampDto
    {
        /// <summary>
        /// Identyfikator ramy czasowej.
        /// </summary>
        public int TimestampId { get; set; }

        /// <summary>
        /// Indeks okienka czasowego w ciągu dnia.
        /// </summary>
        public int PeriodIndex { get; set; }

        /// <summary>
        /// Indeks dnia tygodnia.
        /// </summary>
        [Range(1, 5, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Day { get; set; }

        /// <summary>
        /// Tydzień semestru, którego dotyczy rama czasowa.
        /// </summary>
        public int Week { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="SchedulePosition"/>.
    /// </summary>
    public class SchedulePositionDto
    {
        /// <summary>
        /// Identyfikator pokoju.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Identyfikator ramy czasowej na planie.
        /// </summary>
        public int TimestampId { get; set; }

        /// <summary>
        /// Identyfikator przedmiotu, którego dotyczy pozycja na planie.
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Identyfikator edycji zajęć, której dotyczy pozycja na planie.
        /// </summary>
        public int CourseEditionId { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="CourseEdition"/>.
    /// </summary>
    public class CourseEditionDto
    {
        /// <summary>
        /// Identyfikator przedmiotu, którego dotyczy edycja zajęć.
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Identyfikator edycji zajęć.
        /// </summary>
        public int CourseEditionId { get; set; }

        /// <summary>
        /// Nazwa edycji zajęć.
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="CoordinatorCourseEdition"/>.
    /// </summary>
    public class CoordinatorCourseEditionDto
    {
        /// <summary>
        /// Identyfikator przedmiotu, którego dotyczy przypisanie.
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Identyfikator edycji zajęć, której dotyczy przypisanie.
        /// </summary>
        public int CourseEditionId { get; set; }

        /// <summary>
        /// Identyfikator prowadzącego (użytkownika), którego dotyczy przypisanie.
        /// </summary>
        public int CoordinatorId { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="GroupCourseEdition"/>.
    /// </summary>
    public class GroupCourseEditionDto
    {
        /// <summary>
        /// Identyfikator przedmiotu, którego dotyczy przypisanie.
        /// </summary>
        public int CourseId { get; set; }
        
        /// <summary>
        /// Identyfikator edycji zajęć, której dotyczy przypisanie.
        /// </summary>
        public int CourseEditionId { get; set; }
        
        /// <summary>
        /// Identyfikator grupy, której dotyczy przypisanie.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="Group"/>.
    /// </summary>
    public class GroupDto
    {
        /// <summary>
        /// Identyfikator grupy.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Nazwa grupy.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Identyfikator grupy nadrzędnej.
        /// </summary>
        public int? ParentGroupId { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="Course"/>.
    /// </summary>
    public class CourseDto
    {
        /// <summary>
        /// Identyfikator przedmiotu.
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Identyfikator typu przedmiotu.
        /// </summary>
        public int CourseTypeId { get; set; }

        /// <summary>
        /// Nazwa przedmiotu.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Liczba minut do odbycia w ciągu semestru.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int UnitsMinutes { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="CourseRoom"/>.
    /// </summary>
    public class CourseRoomDto
    {
        /// <summary>
        /// Identyfikator przedmiotu, którego dotyczy przypisanie.
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Identyfikator pokoju, którego dotyczy przypisanie.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Identyfikator użytkownika, który utworzył przypisanie.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="CourseType"/>.
    /// </summary>
    public class CourseTypeDto
    {
        /// <summary>
        /// Identyfikator typu przedmiotu.
        /// </summary>
        public int CourseTypeId { get; set; }

        /// <summary>
        /// Nazwa typu przedmiotu.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Kolor typu przedmiotu (wyświetlany jako tło panelu z zajęciami w interfejsie).
        /// </summary>
        [Required]
        [MaxLength(9)]
        [RegularExpression(@"^#(?:[0-9a-fA-F]{3,4}){1,2}$")]
        public string Color { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="RoomType"/>.
    /// </summary>
    public class RoomTypeDto
    {
        /// <summary>
        /// Identyfikator typu pokoju.
        /// </summary>
        public int RoomTypeId { get; set; }

        /// <summary>
        /// Nazwa typu pokoju.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
        public RoomType FromDto()
        {
            return new RoomType
            {
                RoomTypeId = RoomTypeId,
                Name = Name
            };
        }
    }

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="Room"/>.
    /// </summary>
    public class RoomDto
    {
        /// <summary>
        /// Identyfikator pokoju.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Identyfikator typu pokoju.
        /// </summary>
        public int RoomTypeId { get; set; }

        /// <summary>
        /// Nazwa pokoju.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Pojemność pokoju.
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Capacity { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt transferu danych dla modelu <see cref="StudentGroup"/>.
    /// </summary>
    public class StudentGroupDto
    {
        /// <summary>
        /// Identyfikator grupy, której dotyczy przypisanie.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Identyfikator studenta (użytkownika), którego dotyczy przypisanie.
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// Czy student posiada rolę starosty w grupie.
        /// </summary>
        public bool IsRepresentative { get; set; }

        /// <summary>
        /// Funkcja konwertująca obiekt transferu danych na model.
        /// </summary>
        /// <returns>Utworzony model danych</returns>
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

    /// <summary>
    /// Klasa reprezentująca obiekt przechowujący informacje o pełnej nazwie grupy.
    /// </summary>
    public class GroupFullName
    {
        /// <summary>
        /// Podstawowa nazwa grupy.
        /// </summary>
        public string BasicName { get; set; }
        
        /// <summary>
        /// Pełna nazwa grupy biorąca pod uwagę grupy nadrzędne.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Lista identyfikatorów grup (aktualnej i jej nadrzędnych)
        /// </summary>
        public ICollection<int> GroupsIds { get; set; }

        /// <summary>
        /// Poziom węzła, na którym znajduje się grupa (korzeń - poziom 0).
        /// </summary>
        public int Levels { get; set; }
    }

    /// <summary>
    /// Klasa reprezentująca obiekt przechowujący dużą ilość informacji o grupie.
    /// </summary>
    public class GroupFullInfo
    {
        /// <summary>
        /// Identyfikator wybranej grupy.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Podstawowa nazwa grupy.
        /// </summary>
        public string BasicName { get; set; }

        /// <summary>
        /// Pełna nazwa grupy biorąca pod uwagę grupy nadrzędne.
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// Identyfikatory grup nadrzędnych do wybranej.
        /// </summary>
        public ICollection<int> ParentIds { get; set; }

        /// <summary>
        /// Identyfikatory grup podrzędnych do wybranej.
        /// </summary>
        public ICollection<int> ChildIds { get; set; }
    }

    /// <summary>
    /// Klasa reprezentująca obiekt przechowujący informację o nazwie pokoju.
    /// </summary>
    public class RoomName
    {
        /// <summary>
        /// Nazwa wybranego pokoju.
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Klasa reprezentująca obiekt przechowujący informację o dostępności pokoju.
    /// </summary>
    public class RoomAvailability
    {
        /// <summary>
        /// Identyfikator wybranego pokoju.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Czy jest dostępny.
        /// </summary>
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

    /// <summary>
    /// Klasa reprezentująca obiekt przechowujący dużą ilość informacji o zaplanowanym ruchu w systemie.
    /// </summary>
    public class ScheduledMoveRead : IComparable<ScheduledMoveRead>
    {
        /// <summary>
        /// Identyfikator ruchu.
        /// </summary>
        public int MoveId { get; set; }

        /// <summary>
        /// Czy ruch został potwierdzony do wykonania.
        /// </summary>
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Identyfikator użytkownika, który utworzył ruch.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Kolekcja tygodni źródłowych, których dotyczy ruch.
        /// </summary>
        public IEnumerable<int> SourceWeeks { get; set; }

        /// <summary>
        /// Identyfikator pokoju docelowego, którego dotyczy ruch.
        /// </summary>
        public int DestRoomId { get; set; }

        /// <summary>
        /// Nazwa pokoju docelowego, którego dotyczy ruch.
        /// </summary>
        public string DestRoomName { get; set; }

        /// <summary>
        /// Identyfikator typu pokoju docelowego, którego dotyczy ruch.
        /// </summary>
        public int DestRoomTypeId { get; set; }

        /// <summary>
        /// Indeks docelowego okienka czasowego w ciągu dnia, którego dotyczy ruch.
        /// </summary>
        public int DestPeriodIndex { get; set; }

        /// <summary>
        /// Indeks docelowego dnia w tygodniu, którego dotyczy ruch.
        /// </summary>
        public int DestDay { get; set; }

        /// <summary>
        /// Kolekcja tygodni docelowych, których dotyczy ruch.
        /// </summary>
        public IEnumerable<int> DestWeeks { get; set; }

        /// <summary>
        /// Data i czas utworzenia ruchu w systemie.
        /// </summary>
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
