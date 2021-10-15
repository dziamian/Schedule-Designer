using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class ScheduledMove
    {
        public int RoomId_1 { get; set; }

        public int TimestampId_1 { get; set; }

        public int RoomId_2 { get; set; }

        public int TimestampId_2 { get; set; }


        public int UserId { get; set; }

        public bool RequireConfirmation { get; set; }

        public DateTime ScheduledDate { get; set; }

        
        [ForeignKey("RoomId_1,TimestampId_1")]
        public SchedulePosition Origin { get; set; }

        [ForeignKey("RoomId_2,TimestampId_2")]
        public ScheduleSlot Destination { get; set; }
    }
}
