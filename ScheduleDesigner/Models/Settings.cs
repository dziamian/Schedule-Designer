using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Settings
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 1440, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int CourseDurationMinutes { get; set; }

        [Range(typeof(TimeSpan), "00:00", "23:59")]
        public TimeSpan StartTime { get; set; }

        [Range(typeof(TimeSpan), "00:00", "23:59")]
        [GreaterThan("StartTime")]
        public TimeSpan EndTime { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int TermDurationWeeks { get; set; }
    }
}
