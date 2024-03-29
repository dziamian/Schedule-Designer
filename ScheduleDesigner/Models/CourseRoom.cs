﻿using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class CourseRoom : IExportCsv
    {
        public int CourseId { get; set; }

        public int RoomId { get; set; }

        public int? UserId { get; set; }


        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        [ForeignKey("RoomId")]
        public Room Room { get; set; }

        public virtual ICollection<SchedulePosition> SchedulePositions { get; set; }

        public virtual ICollection<ScheduledMovePosition> ScheduledMoves { get; set; }

        public string GetHeader(string delimiter)
        {
            return $"CourseId{delimiter}RoomId{delimiter}UserId\n";
        }

        public string GetRow(string delimiter)
        {
            return $"{CourseId}{delimiter}{RoomId}{delimiter}{UserId}\n";
        }
    }
}
