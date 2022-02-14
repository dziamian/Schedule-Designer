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
    /// Model danych przechowujący informacje o grupie studenckiej (zajęciowej).
    /// </summary>
    public class Group : IExportCsv
    {
        /// <summary>
        /// Identyfikator grupy studenckiej.
        /// </summary>
        [Key]
        public int GroupId { get; set; }

        /// <summary>
        /// Podstawowa nazwa grupy studenckiej.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Identyfikator grupy nadrzędnej.
        /// </summary>
        public int? ParentGroupId { get; set; }

        /// <summary>
        /// Reprezentacja relacji z grupą nadrzędną.
        /// </summary>
        [ForeignKey("ParentGroupId")]
        public Group ParentGroup { get; set; }

        /// <summary>
        /// Reprezentacja relacji z grupami podrzędnymi.
        /// </summary>
        public virtual ICollection<Group> SubGroups { get; set; }

        /// <summary>
        /// Reprezentacja relacji ze studentami należącymi do danej grupy.
        /// </summary>
        public virtual ICollection<StudentGroup> Students { get; set; }

        /// <summary>
        /// Reprezentacja relacji z edycjami zajęć, które zostały przypisane grupie.
        /// </summary>
        public virtual ICollection<GroupCourseEdition> CourseEditions { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"GroupId{delimiter}Name{delimiter}ParentGroupId\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{GroupId}{delimiter}{Name}{delimiter}{ParentGroupId}\n";
        }
    }
}
