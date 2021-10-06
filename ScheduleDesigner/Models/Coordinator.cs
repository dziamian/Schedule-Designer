using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Coordinator
    {
        public int CoordinatorId { get; set; }

        
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }


        public virtual ICollection<CoordinatorCourseEdition> CourseEditions { get; set; }
    }
}
