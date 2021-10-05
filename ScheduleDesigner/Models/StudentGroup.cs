using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class StudentGroup
    {
        public int ProgrammeId { get; set; }

        public int ProgrammeStageId { get; set; }

        public int ClassId { get; set; }

        public int GroupId { get; set; }

        public int StudentId { get; set; }

        
        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [ForeignKey("ProgrammeId,ProgrammeStageId,ClassId,GroupId")]
        public Group Group { get; set; }
    }
}
