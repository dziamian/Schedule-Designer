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
    /// między grupami a ich studentami.
    /// </summary>
    public class StudentGroup : IExportCsv
    {
        /// <summary>
        /// Identyfikator przypisanej grupy studenckiej.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Identyfikator studenta (użytkownika), który został przypisany do danej grupy.
        /// </summary>
        public int StudentId { get; set; }

        /// <summary>
        /// Informacja czy student (użytkownik) posiada rolę starosty w danej grupie. 
        /// </summary>
        public bool IsRepresentative { get; set; }

        /// <summary>
        /// Reprezentacja relacji ze studentem (użytkownikiem), który został przypisany do danej grupy.
        /// </summary>
        [ForeignKey("StudentId")]
        public User User { get; set; }

        /// <summary>
        /// Reprezentacja relacji z grupą, która została przypisana do studenta.
        /// </summary>
        [ForeignKey("GroupId")]
        public Group Group { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"GroupId{delimiter}StudentId{delimiter}IsRepresentative\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{GroupId}{delimiter}{StudentId}{delimiter}{IsRepresentative}\n";
        }
    }
}
