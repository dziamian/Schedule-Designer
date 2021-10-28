using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Coordinator
    {
        public int UserId { get; set; }

        [MaxLength(100)]
        public string TitleBefore { get; set; }

        [MaxLength(100)]
        public string TitleAfter { get; set; }


        [ForeignKey("UserId")]
        public User User { get; set; }

        public virtual ICollection<CoordinatorCourseEdition> CourseEditions { get; set; }
    }
}
