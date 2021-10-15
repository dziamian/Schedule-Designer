using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class SchedulePosition
    {
        public int RoomId { get; set; }

        public int TimestampId { get; set; }

        public int ProgrammeId { get; set; }

        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        
        public int? LockUserId { get; set; }


        [ForeignKey("ProgrammeId,CourseId,CourseEditionId")]
        public CourseEdition CourseEdition { get; set; }

        [ForeignKey("ProgrammeId,CourseId,RoomId")]
        public CourseRoom CourseRoom { get; set; }

        [ForeignKey("RoomId,TimestampId")]
        public ScheduleSlot ScheduleSlot { get; set; }

        public virtual ICollection<ScheduledMove> ScheduledMoves { get; set; }
    }
}
