using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class CoordinatorCourseEdition : IExportCsv
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }

        public int CoordinatorId { get; set; }


        [ForeignKey("CoordinatorId")]
        public Coordinator Coordinator { get; set; }

        [ForeignKey("CourseId,CourseEditionId")]
        public CourseEdition CourseEdition { get; set; }

        public string GetHeader(string delimiter)
        {
            return $"CourseId{delimiter}CourseEditionId{delimiter}CoordinatorId\n";
        }

        public string GetRow(string delimiter)
        {
            return $"{CourseId}{delimiter}{CourseEditionId}{delimiter}{CoordinatorId}\n";
        }
    }
}
