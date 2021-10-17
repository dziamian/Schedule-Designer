using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class CourseRoomTimestamp
    {
        public int RoomId { get; set; }

        public int TimestampId { get; set; }

        public int CourseId { get; set; }

        
        [ForeignKey("RoomId,CourseId")]
        public CourseRoom CourseRoom { get; set; }

        [ForeignKey("TimestampId")]
        public Timestamp Timestamp { get; set; }

        public SchedulePosition SchedulePosition { get; set; }

        public virtual ICollection<ScheduledMove> ScheduledMovesDestinations { get; set; }
    }
}
