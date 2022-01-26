using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class CourseType : IExportCsv
    {
        [Key]
        public int CourseTypeId { get; set; }

        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        [Required]
        [MaxLength(9)]
        [RegularExpression(@"^#(?:[0-9a-fA-F]{3,4}){1,2}$")]
        public string Color { get; set; }


        public virtual ICollection<Course> Courses { get; set; }

        public string GetHeader(string delimiter)
        {
            return $"CourseTypeId{delimiter}Name{delimiter}Color\n";
        }

        public string GetRow(string delimiter)
        {
            return $"{CourseTypeId}{delimiter}{Name}{delimiter}{Color}\n";
        }
    }
}
