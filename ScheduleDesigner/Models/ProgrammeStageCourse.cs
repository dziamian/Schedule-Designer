using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class ProgrammeStageCourse
    {
        public int ProgrammeId { get; set; }

        public int ProgrammeStageId { get; set; }

        public int CourseId { get; set; }

        
        [ForeignKey("ProgrammeId,ProgrammeStageId")]
        public ProgrammeStage ProgrammeStage { get; set; }

        [ForeignKey("ProgrammeId,CourseId")]
        public Course Course { get; set; }

        public virtual ICollection<GroupCourseEdition> Groups { get; set; }
    }
}
