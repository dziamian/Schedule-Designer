using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class SqlCourseEditionRepo : RepoBase<CourseEdition>, ICourseEditionRepo
    {
        public SqlCourseEditionRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
