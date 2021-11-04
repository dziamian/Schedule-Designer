using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Authentication;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class SqlAuthorizationRepo : RepoBase<Authorization>, IAuthorizationRepo
    {
        public SqlAuthorizationRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
