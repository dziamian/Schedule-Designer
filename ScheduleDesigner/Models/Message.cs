using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje o wiadomościach załączonych do propozycji zmian w planie.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Identyfikator propozycji zmiany.
        /// </summary>
        public int MoveId { get; set; }

        /// <summary>
        /// Treść załączonej wiadomości.
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Content { get; set; }

        /// <summary>
        /// Reprezentacja relacji z propozycją zmiany, do której wiadomość została załączona.
        /// </summary>
        [ForeignKey("MoveId")]
        public ScheduledMove ScheduledMove { get; set; }
    }
}
