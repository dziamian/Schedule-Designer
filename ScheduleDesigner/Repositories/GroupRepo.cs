using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class GroupRepo : RepoBase<Group>, IGroupRepo
    {
        public GroupRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
