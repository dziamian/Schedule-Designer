using ScheduleDesigner.Data_Transform_Objects;
using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Converters
{
    public static class SettingsConverter
    {
        public static SettingsReadDto ToSettingsReadDto(Settings settings)
        {
            return new SettingsReadDto
            {
                UsosBaseUrl = settings.UsosBaseUrl,
                CourseDurationMinutes = settings.CourseDurationMinutes,
                StartTime = settings.StartTime,
                EndTime = settings.EndTime,
                TermDurationWeeks = settings.TermDurationWeeks
            };
        }
    }
}
