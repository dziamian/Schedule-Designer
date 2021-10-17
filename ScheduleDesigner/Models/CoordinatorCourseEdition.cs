using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class CoordinatorCourseEdition
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        public int CoordinatorId { get; set; }


        [ForeignKey("CoordinatorId")]
        public Coordinator Coordinator { get; set; }

        [ForeignKey("CourseId,CourseEditionId")]
        public CourseEdition CourseEdition { get; set; }
    }
}
