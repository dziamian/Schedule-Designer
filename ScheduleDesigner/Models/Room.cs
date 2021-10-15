using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }


        public virtual ICollection<CourseRoom> Courses { get; set; }

        public virtual ICollection<ScheduleSlot> ScheduleSlots { get; set; }
    }
}
