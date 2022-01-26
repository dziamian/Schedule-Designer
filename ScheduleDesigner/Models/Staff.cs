using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Staff : IExportCsv
    {
        public int UserId { get; set; }

        public bool IsAdmin { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        public string GetHeader(string delimiter)
        {
            return $"UserId{delimiter}IsAdmin\n";
        }

        public string GetRow(string delimiter)
        {
            return $"{UserId}{delimiter}{IsAdmin}\n";
        }
    }
}
