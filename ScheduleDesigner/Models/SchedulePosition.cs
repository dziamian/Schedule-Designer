using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ScheduleDesigner.Helpers;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje na temat pojedynczej pozycji na planie.
    /// </summary>
    public class SchedulePosition : IExportCsv
    {
        /// <summary>
        /// Identyfikator pokoju, gdzie planowo mają odbyć się zajęcia.
        /// </summary>
        public int RoomId { get; set; }

        /// <summary>
        /// Identyfikator ramy czasowej, w której planowo mają odbyć się zajęcia.
        /// </summary>
        public int TimestampId { get; set; }

        /// <summary>
        /// Identyfikator przedmiotu, z którego planowo mają odbyć się zajęcia.
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Identyfikator edycji zajęć, które mają się odbyć.
        /// </summary>
        public int CourseEditionId { get; set; }

        /// <summary>
        /// Identyfikator użytkownika, który zablokował dostęp do pozycji na planie w systemie.
        /// </summary>
        public int? LockUserId { get; set; }

        /// <summary>
        /// Identyfikator połączenia użytkownika, który zablokował dostęp do pozycji na planie w systemie.
        /// </summary>
        [MaxLength(50)]
        public string LockUserConnectionId { get; set; }

        /// <summary>
        /// Reprezentacja relacji z użytkownikiem, który zablokował zasób.
        /// </summary>
        [ForeignKey("LockUserId")]
        public User LockUser { get; set; }

        /// <summary>
        /// Reprezentacja relacji z edycją zajęć, które planowo mają się odbyć.
        /// </summary>
        [ForeignKey("CourseId,CourseEditionId")]
        public CourseEdition CourseEdition { get; set; }

        /// <summary>
        /// Reprezentacja relacji z przypisanym pokojem do przedmiotu, z którego zajęcia mają się odbyć.
        /// </summary>
        [ForeignKey("RoomId,CourseId")]
        public CourseRoom CourseRoom { get; set; }

        /// <summary>
        /// Reprezentacja relacji z ramą czasową, w której planowo mają odbyć się zajęcia.
        /// </summary>
        [ForeignKey("TimestampId")]
        public Timestamp Timestamp { get; set; }

        /// <summary>
        /// Reprezentacja relacji z zaplanowanymi zmianami i propozycjami, które zostały przypisane do danej pozycji na planie.
        /// </summary>
        public virtual ICollection<ScheduledMovePosition> ScheduledMovePositions { get; set; }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetHeader"/>).
        /// Pobranie nagłówków właściwości modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi nagłówkami</param>
        /// <returns>Pojedynczy wiersz z nagłówkami</returns>
        public string GetHeader(string delimiter)
        {
            return $"RoomId{delimiter}TimestampId{delimiter}CourseId{delimiter}CourseEditionId\n";
        }

        /// <summary>
        /// Implementacja metody interfejsu (<see cref="IExportCsv.GetRow"/>).
        /// Pobranie danych z modelu.
        /// </summary>
        /// <param name="delimiter">Ogranicznik między poszczególnymi danymi</param>
        /// <returns>Pojedynczy wiersz z danymi</returns>
        public string GetRow(string delimiter)
        {
            return $"{RoomId}{delimiter}{TimestampId}{delimiter}{CourseId}{delimiter}{CourseEditionId}\n";
        }
    }
}
