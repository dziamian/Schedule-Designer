using ScheduleDesigner.Authentication;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class User
    {
        public int UserId { get; set; }

        
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        
        public Student Student { get; set; }

        public Coordinator Coordinator { get; set; }

        public Staff Staff { get; set; }

        public Authorization Authorization { get; set; }

        public virtual ICollection<CourseRoom> CourseRoomsPropositions { get; set; }

        public virtual ICollection<ScheduledMove> ScheduledMoves { get; set; }

        public virtual ICollection<SchedulePosition> LockedPositions { get; set; }

        public virtual ICollection<CourseEdition> LockedCourseEditions { get; set; }
    }
}
