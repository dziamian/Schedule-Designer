using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ScheduleDesigner.Models
{
    public class ScheduledMove
    {
        [Key]
        public int MoveId { get; set; }

        [Required]
        public int UserId { get; set; }

        public bool IsConfirmed { get; set; }

        [Required]
        public DateTime ScheduleOrder { get; set; }

        [MaxLength(300)]
        public string Message { get; set; }


        [ForeignKey("UserId")]
        public User User { get; set; }

        public virtual ICollection<ScheduledMovePosition> ScheduledPositions { get; set; }
    }
}
