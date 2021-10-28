using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class GroupCourseEdition
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        public int GroupId { get; set; }


        [ForeignKey("GroupId")]
        public Group Group { get; set; }

        [ForeignKey("CourseId,CourseEditionId")]
        public CourseEdition CourseEdition { get; set; }
    }
}
