using System;
using System.Collections.Generic;

namespace ScheduleDesigner.Dtos
{
    public class ScheduledMoveRead : IComparable<ScheduledMoveRead>
    {
        public int MoveId { get; set; }

        public bool IsConfirmed { get; set; }

        public IEnumerable<int> SourceWeeks { get; set; }

        public int DestRoomId { get; set; }

        public string DestRoomName { get; set; }

        public int DestRoomTypeId { get; set; }

        public int DestPeriodIndex { get; set; }

        public int DestDay { get; set; }

        public IEnumerable<int> DestWeeks { get; set; }

        public DateTime ScheduleOrder { get; set; }

        public int CompareTo(ScheduledMoveRead other)
        {
            int result = this.ScheduleOrder.CompareTo(other.ScheduleOrder);
            if (result != 0)
            {
                return result;
            }
            return 0;
        }
    }
}
