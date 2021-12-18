using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Room
    {
        [Key]
        public int RoomId { get; set; }

        public int RoomTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }


        [ForeignKey("RoomTypeId")]
        public RoomType Type { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Capacity { get; set; }

        public virtual ICollection<CourseRoom> Courses { get; set; }
    }
}
