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
    /// Model danych przechowujący informacje na temat przedmiotu.
    /// </summary>
    public class Course : IExportCsv
    {
        /// <summary>
        /// Identyfikator przedmiotu.
        /// </summary>
        [Key]
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
        /// Liczba minut zajęć do odbycia w ciągu semestru (przeliczane na jednostki zajęciowe).
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int UnitsMinutes { get; set; }

        /// <summary>
        /// Reprezentacja relacji z typem przedmiotu.
        /// </summary>
        
        [ForeignKey("CourseTypeId")]
        public CourseType CourseType { get; set; }

        /// <summary>
        /// Reprezentacja relacji z edycjami zajęć.
        /// </summary>
        public virtual ICollection<CourseEdition> Editions { get; set; }

        /// <summary>
        /// Reprezentacja relacji z pokojami, w których mogą odbywać się zajęcia z danego przedmiotu.
        /// </summary>
        public virtual ICollection<CourseRoom> Rooms { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"CourseId{delimiter}CourseTypeId{delimiter}Name\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{CourseId}{delimiter}{CourseTypeId}{delimiter}{Name}\n";
        }
    }
}
