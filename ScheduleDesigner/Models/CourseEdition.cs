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
    /// Model danych przechowujący informacje na temat edycji zajęć.
    /// </summary>
    public class CourseEdition : IExportCsv
    {
        /// <summary>
        /// Identyfikator przedmiotu.
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
        /// Identyfikator użytkownika, który zablokował dostęp do edycji zajęć w systemie.
        /// </summary>
        public int? LockUserId { get; set; }

        /// <summary>
        /// Identyfikator połączenia użytkownika, który zablokował dostęp do edycji zajęć w systemie.
        /// </summary>
        [MaxLength(50)]
        public string LockUserConnectionId { get; set; }

        /// <summary>
        /// Reprezentacja relacji z użytkownikiem, który zablokował zasób.
        /// </summary>
        [ForeignKey("LockUserId")]
        public User LockUser { get; set; }

        /// <summary>
        /// Reprezentacja relacji z przedmiotem, dla którego utworzona została edycja zajęć.
        /// </summary>
        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        /// <summary>
        /// Reprezentacja relacji z prowadzącymi (użytkownikami) zajęcia.
        /// </summary>
        public virtual ICollection<CoordinatorCourseEdition> Coordinators { get; set; }
        
        /// <summary>
        /// Reprezentacja relacji z grupami, które mają odbyć zajęcia.
        /// </summary>
        public virtual ICollection<GroupCourseEdition> Groups { get; set; }

        /// <summary>
        /// Reprezentacja relacji z pozycjami zajęć w planie.
        /// </summary>
        public virtual ICollection<SchedulePosition> SchedulePositions { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"CourseId{delimiter}CourseEditionId{delimiter}Name\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{CourseId}{delimiter}{CourseEditionId}{delimiter}{Name}\n";
        }
    }
}
