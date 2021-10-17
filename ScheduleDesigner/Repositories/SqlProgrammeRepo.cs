using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories
{
    public class SqlProgrammeRepo : RepoBase<Programme>, IProgrammeRepo
    {
        public SqlProgrammeRepo(ScheduleDesignerDbContext context)
            :base(context)
        { }
    }
}
