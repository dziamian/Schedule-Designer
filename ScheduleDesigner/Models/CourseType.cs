using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje o typie przedmiotu.
    /// </summary>
    public class CourseType : IExportCsv
    {
        /// <summary>
        /// Identyfikator typu przedmiotu.
        /// </summary>
        [Key]
        public int CourseTypeId { get; set; }

        /// <summary>
        /// Nazwa typu przedmiotu.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Kolor typu przedmiotu zapisany w formie heksadecymalnej (używany jako tło panelu z zajęciami w aplikacji klienckiej).
        /// </summary>
        [Required]
        [MaxLength(9)]
        [RegularExpression(@"^#(?:[0-9a-fA-F]{3,4}){1,2}$")]
        public string Color { get; set; }

        /// <summary>
        /// Reprezentacja relacji z przedmiotami posiadającymi dany typ.
        /// </summary>
        public virtual ICollection<Course> Courses { get; set; }
        
        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"CourseTypeId{delimiter}Name{delimiter}Color\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{CourseTypeId}{delimiter}{Name}{delimiter}{Color}\n";
        }
    }
}
