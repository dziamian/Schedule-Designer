using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Class
    {
        public int ProgrammeId { get; set; }

        public int ProgrammeStageId { get; set; }

        public int ClassId { get; set; }

        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        
        [ForeignKey("ProgrammeId,ProgrammeStageId")]
        public ProgrammeStage ProgrammeStage { get; set; }

        public virtual ICollection<Group> Groups { get; set; }
    }
}
