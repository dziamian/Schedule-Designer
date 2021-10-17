﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class SqlGroupCourseEditionRepo : RepoBase<GroupCourseEdition>, IGroupCourseEditionRepo
    {
        public SqlGroupCourseEditionRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
