using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Dtos
{
    public class RoomName
    {
        public string Name { get; set; }
    }

    public class RoomAvailability
    {
        public int RoomId { get; set; }
        public bool IsBusy { get; set; }
    }
}
