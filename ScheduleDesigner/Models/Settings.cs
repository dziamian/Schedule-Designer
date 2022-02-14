using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    /// <summary>
    /// Model danych przechowujący informacje na temat ustawień aplikacji.
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Identyfikator ustawień.
        /// </summary>
        [Key]
        public int Id { get; set; }
        
        /// <summary>
        /// Liczba minut dla pojedynczej jednostki zajęciowej (pojedyncze okienko czasowe w ciągu dnia).
        /// </summary>
        [Range(1, 1440, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int CourseDurationMinutes { get; set; }

        /// <summary>
        /// Godzina możliwego rozpoczęcia pierwszych zajęć w planie w ciągu dnia.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00", "23:59", ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// Godzina możliwego zakończenia ostatnich zajęć w planie w ciągu dnia.
        /// </summary>
        [Range(typeof(TimeSpan), "00:00", "23:59", ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        [GreaterThan("StartTime")]
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Liczba tygodni w semestrze.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int TermDurationWeeks { get; set; }
    }
}
