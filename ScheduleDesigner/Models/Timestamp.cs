using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Timestamp
    {
        [Key]
        public int TimestampId { get; set; }


        public int PeriodIndex { get; set; }

        [Range(1,5, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Day { get; set; }

        public int Week { get; set; }


        public virtual ICollection<CourseRoomTimestamp> CourseRoomTimestamps { get; set; }
    }
}
