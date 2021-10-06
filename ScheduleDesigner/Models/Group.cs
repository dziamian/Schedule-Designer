using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Group
    {
        public int ProgrammeId { get; set; }

        public int ProgrammeStageId { get; set; }

        public int ClassId { get; set; }

        public int GroupId { get; set; }


        [Required]
        [MaxLength(100)]
        public string Name { get; set; }


        [ForeignKey("ProgrammeId,ProgrammeStageId,ClassId")]
        public Class Class { get; set; }

        public virtual ICollection<StudentGroup> Students { get; set; }

        public virtual ICollection<GroupCourseEdition> CourseEditions { get; set; }
    }
}
