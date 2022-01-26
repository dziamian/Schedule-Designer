using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class StudentGroup : IExportCsv
    {
        public int GroupId { get; set; }

        public int StudentId { get; set; }

        public bool IsRepresentative { get; set; }


        [ForeignKey("StudentId")]
        public Student Student { get; set; }

        [ForeignKey("GroupId")]
        public Group Group { get; set; }

        public string GetHeader(string delimiter)
        {
            return $"GroupId{delimiter}StudentId{delimiter}IsRepresentative\n";
        }

        public string GetRow(string delimiter)
        {
            return $"{GroupId}{delimiter}{StudentId}{delimiter}{IsRepresentative}\n";
        }
    }
}
