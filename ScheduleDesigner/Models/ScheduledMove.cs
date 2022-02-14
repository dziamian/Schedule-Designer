using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje na temat utworzonej w systemie zaplanowanej zmiany lub propozycji.
    /// </summary>
    public class ScheduledMove
    {
        /// <summary>
        /// Identyfikator ruchu.
        /// </summary>
        [Key]
        public int MoveId { get; set; }

        /// <summary>
        /// Identyfikator użytkownika, który utworzył dany ruch.
        /// </summary>
        [Required]
        public int UserId { get; set; }

        /// <summary>
        /// Czy ruch został potwierdzony do wykonania (jeśli tak - zaplanowana zmiana, w przeciwnym wypadku - propozycja zmiany)
        /// </summary>
        public bool IsConfirmed { get; set; }

        /// <summary>
        /// Data utworzenia danego ruchu w systemie.
        /// </summary>
        [Required]
        public DateTime ScheduleOrder { get; set; }

        /// <summary>
        /// Reprezentacja relacji z użytkownikiem, który utworzył ruch w systemie.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; }

        /// <summary>
        /// Reprezentacja relacji z wiadomością, która ewentualnie została załączona do propozycji zmiany.
        /// </summary>
        public Message Message { get; set; }

        /// <summary>
        /// Reprezentacja relacji z pojedynczymi przesunięciami składającymi się na pełny ruch w planie.
        /// </summary>
        public virtual ICollection<ScheduledMovePosition> ScheduledPositions { get; set; }
    }
}
