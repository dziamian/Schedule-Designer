using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Course
    {
        public int ProgrammeId { get; set; }

        public int CourseId { get; set; }

       
        public int CourseTypeId { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int UnitsMinutes { get; set; }

        
        [ForeignKey("ProgrammeId")]
        public Programme Programme { get; set; }
        
        [ForeignKey("CourseTypeId")]
        public CourseType CourseType { get; set; }

        public virtual ICollection<ProgrammeStageCourse> ProgrammeStageCourses { get; set; }

        public virtual ICollection<CourseEdition> Editions { get; set; }

        public virtual ICollection<CourseRoom> Rooms { get; set; }
    }
}
