using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Authentication;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class AuthorizationRepo : RepoBase<Authorization>, IAuthorizationRepo
    {
        public AuthorizationRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
