using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje o typie pokoju.
    /// </summary>
    public class RoomType : IExportCsv
    {
        /// <summary>
        /// Identyfikator typu pokoju.
        /// </summary>
        [Key]
        public int RoomTypeId { get; set; }

        /// <summary>
        /// Nazwa typu pokoju.
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        /// <summary>
        /// Reprezentacja relacji z pokojami posiadającymi dany typ.
        /// </summary>
        public virtual ICollection<Room> Rooms { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"RoomTypeId{delimiter}Name\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{RoomTypeId}{delimiter}{Name}\n";
        }
    }
}
