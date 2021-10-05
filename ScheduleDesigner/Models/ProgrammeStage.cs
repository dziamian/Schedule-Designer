using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class ProgrammeStage
    {
        public int ProgrammeId { get; set; }
        
        public int ProgrammeStageId { get; set; }


        [Required]
        [MaxLength(100)]
        public string Name { get; set; }


        [ForeignKey("ProgrammeId")]
        public Programme Programme { get; set; }

        public virtual ICollection<ProgrammeStageCourse> ProgrammeStageCourses { get; set; }

        public virtual ICollection<Class> Classes { get; set; }
    }
}
