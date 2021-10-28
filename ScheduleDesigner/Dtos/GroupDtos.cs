using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Dtos
{
    public class GroupFullName
    {
        public string FullName { get; set; }
        public ICollection<int> GroupIds { get; set; }
        public int Levels { get; set; }
    }
}
