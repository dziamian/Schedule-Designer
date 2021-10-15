using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Data_Transform_Objects
{
    public class SettingsReadDto
    {
        public int CourseDurationMinutes { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int TermDurationWeeks { get; set; }
    }
}
