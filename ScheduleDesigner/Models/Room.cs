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
    /// Model danych przechowujący informacje o pokoju.
    /// </summary>
    public class Room : IExportCsv
    {
        /// <summary>
        /// Identyfikator pokoju.
        /// </summary>
        [Key]
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
        /// Reprezentacja relacji z typem pokoju.
        /// </summary>
        [ForeignKey("RoomTypeId")]
        public RoomType Type { get; set; }

        /// <summary>
        /// Reprezentacja relacji z przedmiotami, które zostały przypisane jako możliwe do odbycia w danym pokoju.
        /// </summary>
        public virtual ICollection<CourseRoom> Courses { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"RoomId{delimiter}RoomTypeId{delimiter}Name{delimiter}Capacity\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{RoomId}{delimiter}{RoomTypeId}{delimiter}{Name}{delimiter}{Capacity}\n";
        }
    }
}
