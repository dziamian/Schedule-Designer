using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static ScheduleDesigner.Helpers;

namespace ScheduleDesigner.Models
{
    public class SchedulePosition : IExportCsv
    {
        public int RoomId { get; set; }

        public int TimestampId { get; set; }

        public int CourseId { get; set; }

        
        public int CourseEditionId { get; set; }

        public int? LockUserId { get; set; }

        [MaxLength(50)]
        public string LockUserConnectionId { get; set; }


        [ForeignKey("LockUserId")]
        public User LockUser { get; set; }

        [ForeignKey("CourseId,CourseEditionId")]
        public CourseEdition CourseEdition { get; set; }

        [ForeignKey("RoomId,CourseId")]
        public CourseRoom CourseRoom { get; set; }

        [ForeignKey("TimestampId")]
        public Timestamp Timestamp { get; set; }

        public virtual ICollection<ScheduledMovePosition> ScheduledMovePositions { get; set; }

        public string GetHeader()
        {
            return $"RoomId|TimestampId|CourseId|CourseEditionId\n";
        }

        public string GetRow()
        {
            return $"{RoomId}|{TimestampId}|{CourseId}|{CourseEditionId}\n";
        }
    }
}
