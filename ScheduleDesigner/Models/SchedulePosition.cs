using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class SchedulePosition
    {
        [Key]
        public int SchedulePositionId { get; set; }

        [Index("IX_RoomId_TimestampId", 1, IsUnique = true)]
        public int RoomId { get; set; }

        [Index("IX_RoomId_TimestampId", 2, IsUnique = true)]
        public int TimestampId { get; set; }

        public int ProgrammeId { get; set; }

        public int CourseId { get; set; }

        public int CourseTypeId { get; set; }

        public int CourseEditionId { get; set; }


        [ForeignKey("ProgrammeId,CourseId,CourseTypeId,CourseEditionId")]
        public CourseEdition CourseEdition { get; set; }

        [ForeignKey("TimestampId")]
        public Timestamp Timestamp { get; set; }

        [ForeignKey("ProgrammeId,CourseId,CourseTypeId,RoomId")]
        public CourseRoom CourseRoom { get; set; }
    }
}
