using ScheduleDesigner.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class RoomType : IExportCsv
    {
        [Key]
        public int RoomTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        
        public virtual ICollection<Room> Rooms { get; set; }

        public string GetHeader(string delimiter)
        {
            return $"RoomTypeId{delimiter}Name\n";
        }

        public string GetRow(string delimiter)
        {
            return $"{RoomTypeId}{delimiter}{Name}\n";
        }
    }
}
