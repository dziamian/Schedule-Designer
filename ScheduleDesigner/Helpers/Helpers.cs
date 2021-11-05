using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Helpers
{
    public class ApplicationInfo
    {
        public string BaseUsosUrl { get; set; }
    }

    public class Consumer
    {
        public string Key { get; set; }

        public string Secret { get; set; }
    }

    public class CourseEditionKey
    {
        public int CourseId { get; set; }

        public int CourseEditionId { get; set; }


        public bool Equals(CourseEditionKey key)
        {
            return key.CourseId.Equals(CourseId) && key.CourseEditionId.Equals(CourseEditionId);
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as CourseEditionKey);
        }

        public override int GetHashCode()
        {
            return CourseId.GetHashCode() ^ CourseEditionId.GetHashCode();
        }
    }
}
