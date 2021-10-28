using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class CourseEdition
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }


        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        public int? LockUserId { get; set; }

        
        [ForeignKey("LockUserId")]
        public User LockUser { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        public virtual ICollection<CoordinatorCourseEdition> Coordinators { get; set; }
        
        public virtual ICollection<GroupCourseEdition> Groups { get; set; }

        public virtual ICollection<SchedulePosition> SchedulePositions { get; set; }
    }
}
