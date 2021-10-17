using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class CourseRoom
    {
        public int CourseId { get; set; }

        public int RoomId { get; set; }


        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        [ForeignKey("RoomId")]
        public Room Room { get; set; }

        public virtual ICollection<CourseRoomTimestamp> Timestamps { get; set; }
    }
}
