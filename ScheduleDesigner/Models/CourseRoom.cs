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
    /// między przedmiotami a pokojami, w których mogą się one odbywać.
    /// </summary>
    public class CourseRoom : IExportCsv
    {
        /// <summary>
        /// Identyfikator przedmiotu.
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Identyfikator pokoju.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Identyfikator użytkownika, który dodał przypisanie pokoju do przedmiotu.
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Reprezentacja relacji z użytkownikiem, który utworzył przypisanie.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; }

        /// <summary>
        /// Reprezentacja relacji z przedmiotem, którego przypisanie dotyczy.
        /// </summary>
        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        /// <summary>
        /// Reprezentacja relacji z pokojem, którego przypisanie dotyczy.
        /// </summary>
        [ForeignKey("RoomId")]
        public Room Room { get; set; }

        /// <summary>
        /// Reprezentacja relacji z pozycjami w planie, które dotyczą konkretnego przedmiotu i pokoju (które zostały przypisane).
        /// </summary>
        public virtual ICollection<SchedulePosition> SchedulePositions { get; set; }

        /// <summary>
        /// Reprezentacja relacji z docelowymi pozycjami zaplanowanych ruchów w systemie.
        /// </summary>
        public virtual ICollection<ScheduledMovePosition> ScheduledMoves { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"CourseId{delimiter}RoomId{delimiter}UserId\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{CourseId}{delimiter}{RoomId}{delimiter}{UserId}\n";
        }
    }
}
