using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleDesigner.Models
{
    public class Message
    {
        public int MoveId { get; set; }

        [Required]
        [MaxLength(300)]
        public string Content { get; set; }

        [ForeignKey("MoveId")]
        public ScheduledMove ScheduledMove { get; set; }
    }
}
