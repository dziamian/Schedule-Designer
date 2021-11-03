﻿using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories
{
    public class SqlCourseTypeRepo : RepoBase<CourseType>, ICourseTypeRepo
    {
        public SqlCourseTypeRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}