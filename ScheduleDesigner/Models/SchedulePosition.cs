using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class SchedulePosition
    {
        public int RoomId { get; set; }

        public int TimestampId { get; set; }

        public int ProgrammeId { get; set; }

        public int CourseId { get; set; }

        public int CourseTypeId { get; set; }

        public int CourseEditionId { get; set; }

        public int? LockUserId { get; set; }


        [ForeignKey("ProgrammeId,CourseId,CourseTypeId,CourseEditionId")]
        public CourseEdition CourseEdition { get; set; }

        [ForeignKey("TimestampId")]
        public Timestamp Timestamp { get; set; }

        [ForeignKey("ProgrammeId,CourseId,CourseTypeId,RoomId")]
        public CourseRoom CourseRoom { get; set; }
    }
}
