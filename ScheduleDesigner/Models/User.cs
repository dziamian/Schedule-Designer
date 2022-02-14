using ScheduleDesigner.Authentication;
using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje o użytkowniku systemu.
    /// </summary>
    public class User : IExportCsv
    {
        /// <summary>
        /// Identyfikator użytkownika (pobierany z zewnętrznego systemu USOS).
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Numer przypisany studentowi lub pracownikowi uczelni.
        /// </summary>
        [MaxLength(50)]
        public string AcademicNumber { get; set; }

        /// <summary>
        /// Imię użytkownika.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        /// <summary>
        /// Nazwisko użytkownika.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        /// <summary>
        /// Tytuły naukowe wypisywane przed nazwiskiem użytkownika.
        /// </summary>
        [MaxLength(100)]
        public string TitleBefore { get; set; }

        /// <summary>
        /// Tytuły naukowe wypisywane po nazwisku użytkownika.
        /// </summary>
        [MaxLength(100)]
        public string TitleAfter { get; set; }

        /// <summary>
        /// Określa czy użytkownik posiada rolę studenta w systemie.
        /// </summary>
        public bool IsStudent { get; set; }

        /// <summary>
        /// Określa czy użytkownik posiada rolę pracownika w systemie.
        /// </summary>
        public bool IsStaff { get; set; }

        /// <summary>
        /// Określa czy użytkownik posiada rolę prowadzącego w systemie.
        /// </summary>
        public bool IsCoordinator { get; set; }

        /// <summary>
        /// Określa czy użytkownik posiada rolę administratora w systemie.
        /// </summary>
        public bool IsAdmin { get; set; }

        /// <summary>
        /// Reprezentacja relacji z informacjami o tokenie dostępu użytkownika do systemu USOS.
        /// </summary>
        public Authorization Authorization { get; set; }

        /// <summary>
        /// Reprezentacja relacji z grupami, do których należy użytkownik (student).
        /// </summary>
        public virtual ICollection<StudentGroup> Groups { get; set; }

        /// <summary>
        /// Reprezentacja relacji z edycjami zajęć, które prowadzi użytkownik (prowadzący).
        /// </summary>
        public virtual ICollection<CoordinatorCourseEdition> CourseEditions { get; set; }

        /// <summary>
        /// Reprezentacja relacji z przypisanymi pokojami do przedmiotów przez użytkownika.
        /// </summary>
        public virtual ICollection<CourseRoom> CourseRoomsPropositions { get; set; }

        /// <summary>
        /// Reprezentacja relacji z utworzonymi zaplanowanymi zmianami w planie i propozycjami przez użytkownika.
        /// </summary>
        public virtual ICollection<ScheduledMove> ScheduledMoves { get; set; }

        /// <summary>
        /// Reprezentacja relacji z zablokowanymi zasobami dotyczącymi pozycji w planie.
        /// </summary>
        public virtual ICollection<SchedulePosition> LockedPositions { get; set; }

        /// <summary>
        /// Reprezentacja relacji z zablokowanymi zasobami dotyczącymi edycji zajęć.
        /// </summary>
        public virtual ICollection<CourseEdition> LockedCourseEditions { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"UserId{delimiter}AcademicNumber{delimiter}FirstName{delimiter}" +
                $"LastName{delimiter}TitleBefore{delimiter}TitleAfter{delimiter}" +
                $"IsStudent{delimiter}IsStaff{delimiter}IsCoordinator{delimiter}IsAdmin\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{UserId}{delimiter}{AcademicNumber}{delimiter}{FirstName}{delimiter}" +
                $"{LastName}{delimiter}{TitleBefore}{delimiter}{TitleAfter}{delimiter}" +
                $"{IsStudent}{delimiter}{IsStaff}{delimiter}{IsCoordinator}{delimiter}{IsAdmin}\n";
        }
    }
}
