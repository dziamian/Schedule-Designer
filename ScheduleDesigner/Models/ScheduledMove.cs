using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class ScheduledMove
    {
        public int MoveId { get; set; }

        public int RoomId_1 { get; set; }

        public int TimestampId_1 { get; set; }

        public int RoomId_2 { get; set; }

        public int TimestampId_2 { get; set; }

        public int CourseId { get; set; }


        public int UserId { get; set; }

        public bool IsConfirmed { get; set; }

        public DateTime ScheduleOrder { get; set; }

        
        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("RoomId_1,TimestampId_1,CourseId")]
        public SchedulePosition Origin { get; set; }

        [ForeignKey("RoomId_2,TimestampId_2,CourseId")]
        public CourseRoomTimestamp Destination { get; set; }
    }
}
