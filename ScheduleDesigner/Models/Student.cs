using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Student
    {
        public int UserId { get; set; }

        public string StudentNumber { get; set; }


        [ForeignKey("UserId")]
        public User User { get; set; }

        public virtual ICollection<StudentGroup> Groups { get; set; }
    }
}
