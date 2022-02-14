using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje na temat pojedynczego przesunięcia w planie (może być częścią pełnego ruchu).
    /// </summary>
    public class ScheduledMovePosition
    {
        /// <summary>
        /// Identyfikator ruchu.
        /// </summary>
        public int MoveId { get; set; }

        /// <summary>
        /// Identyfikator pokoju źródłowego (z którego ma się odbyć przesunięcie).
        /// </summary>
        public int RoomId_1 { get; set; }

        /// <summary>
        /// Identyfikator źródłowej ramy czasowej (z której ma się odbyć przesunięcie).
        /// </summary>
        public int TimestampId_1 { get; set; }

        /// <summary>
        /// Identyfikator pokoju docelowego (do którego ma się odbyć przesunięcie).
        /// </summary>
        public int RoomId_2 { get; set; }

        /// <summary>
        /// Identyfikator docelowej ramy czasowej (do której ma się odbyć przesunięcie).
        /// </summary>
        public int TimestampId_2 { get; set; }

        /// <summary>
        /// Identyfikator przedmiotu, którego dotyczy przesunięcie.
        /// </summary>
        public int CourseId { get; set; }

        /// <summary>
        /// Reprezentacja relacji z danym ruchem.
        /// </summary>
        [ForeignKey("MoveId")]
        public ScheduledMove ScheduledMove { get; set; }

        /// <summary>
        /// Reprezentacja relacji ze źródłową pozycją w planie.
        /// </summary>
        [ForeignKey("RoomId_1,TimestampId_1,CourseId")]
        public SchedulePosition Origin { get; set; }

        /// <summary>
        /// Reprezentacja relacji z przypisanym docelowym pokojem z przedmiotem, którego dotyczy przesunięcie.
        /// </summary>
        [ForeignKey("RoomId_2,CourseId")]
        public CourseRoom DestinationRoom { get; set; }

        /// <summary>
        /// Reprezentacja relacji z docelową ramą czasową.
        /// </summary>
        [ForeignKey("TimestampId_2")]
        public Timestamp DestinationTimestamp { get; set; }
    }
}
