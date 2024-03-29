﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class ScheduledMovePosition
    {
        public int MoveId { get; set; }

        public int RoomId_1 { get; set; }

        public int TimestampId_1 { get; set; }

        public int RoomId_2 { get; set; }

        public int TimestampId_2 { get; set; }

        public int CourseId { get; set; }

        [ForeignKey("MoveId")]
        public ScheduledMove ScheduledMove { get; set; }

        [ForeignKey("RoomId_1,TimestampId_1,CourseId")]
        public SchedulePosition Origin { get; set; }

        [ForeignKey("RoomId_2,CourseId")]
        public CourseRoom DestinationRoom { get; set; }

        [ForeignKey("TimestampId_2")]
        public Timestamp DestinationTimestamp { get; set; }
    }
}
