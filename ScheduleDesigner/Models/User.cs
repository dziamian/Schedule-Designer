using ScheduleDesigner.Authentication;
using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class User : IExportCsv
    {
        public int UserId { get; set; }

        [MaxLength(50)]
        public string AcademicNumber { get; set; }

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; }

        [MaxLength(100)]
        public string TitleBefore { get; set; }

        [MaxLength(100)]
        public string TitleAfter { get; set; }

        public bool IsStudent { get; set; }

        public bool IsStaff { get; set; }

        public bool IsCoordinator { get; set; }

        public bool IsAdmin { get; set; }

        public Authorization Authorization { get; set; }

        public virtual ICollection<StudentGroup> Groups { get; set; }

        public virtual ICollection<CoordinatorCourseEdition> CourseEditions { get; set; }

        public virtual ICollection<CourseRoom> CourseRoomsPropositions { get; set; }

        public virtual ICollection<ScheduledMove> ScheduledMoves { get; set; }

        public virtual ICollection<SchedulePosition> LockedPositions { get; set; }

        public virtual ICollection<CourseEdition> LockedCourseEditions { get; set; }

        public string GetHeader(string delimiter)
        {
            return $"UserId{delimiter}AcademicNumber{delimiter}FirstName{delimiter}" +
                $"LastName{delimiter}TitleBefore{delimiter}TitleAfter{delimiter}" +
                $"IsStudent{delimiter}IsStaff{delimiter}IsCoordinator{delimiter}IsAdmin\n";
        }

        public string GetRow(string delimiter)
        {
            return $"{UserId}{delimiter}{AcademicNumber}{delimiter}{FirstName}{delimiter}" +
                $"{LastName}{delimiter}{TitleBefore}{delimiter}{TitleAfter}{delimiter}" +
                $"{IsStudent}{delimiter}{IsStaff}{delimiter}{IsCoordinator}{delimiter}{IsAdmin}\n";
        }
    }
}
