using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class SqlRoomRepo : RepoBase<Room>, IRoomRepo
    {
        public SqlRoomRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
