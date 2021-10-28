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
        [Key]
        public int GroupId { get; set; }


        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public int? ParentGroupId { get; set; }


        [ForeignKey("ParentGroupId")]
        public Group ParentGroup { get; set; }

        public virtual ICollection<Group> SubGroups { get; set; }

        public virtual ICollection<StudentGroup> Students { get; set; }

        public virtual ICollection<GroupCourseEdition> CourseEditions { get; set; }
    }
}
