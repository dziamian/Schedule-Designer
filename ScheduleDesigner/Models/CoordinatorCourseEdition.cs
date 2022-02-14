using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje o powiązaniach 
    /// między prowadzącymi (użytkownikami) a ich edycjami zajęć.
    /// </summary>
    public class CoordinatorCourseEdition : IExportCsv
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
        /// Identyfikator przypisanego prowadzącego (użytkownika).
        /// </summary>
        public int CoordinatorId { get; set; }

        /// <summary>
        /// Reprezentacja relacji z użytkownikiem.
        /// </summary>
        [ForeignKey("CoordinatorId")]
        public User User { get; set; }

        /// <summary>
        /// Reprezentacja relacji z edycją zajęć.
        /// </summary>
        [ForeignKey("CourseId,CourseEditionId")]
        public CourseEdition CourseEdition { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"CourseId{delimiter}CourseEditionId{delimiter}CoordinatorId\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{CourseId}{delimiter}{CourseEditionId}{delimiter}{CoordinatorId}\n";
        }
    }
}
