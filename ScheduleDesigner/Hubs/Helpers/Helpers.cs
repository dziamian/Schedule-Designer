using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Hubs.Helpers
{
    public class MessageObject
    {
        public int StatusCode { get; set; }

        public string Message { get; set; }

        public override bool Equals(object obj)
        {
            return obj is MessageObject @object &&
                   StatusCode == @object.StatusCode &&
                   Message == @object.Message;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StatusCode, Message);
        }
    }
}
