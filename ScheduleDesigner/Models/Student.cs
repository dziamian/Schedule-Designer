using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Student : IExportCsv
    {
        public int UserId { get; set; }

        [MaxLength(20)]
        public string StudentNumber { get; set; }


        [ForeignKey("UserId")]
        public User User { get; set; }

        public virtual ICollection<StudentGroup> Groups { get; set; }

        public string GetHeader(string delimiter)
        {
            return $"UserId{delimiter}StudentNumber\n";
        }

        public string GetRow(string delimiter)
        {
            return $"{UserId}{delimiter}{StudentNumber}\n";
        }
    }
}
