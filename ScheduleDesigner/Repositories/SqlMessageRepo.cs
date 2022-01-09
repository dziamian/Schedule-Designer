using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class SqlMessageRepo : RepoBase<Message>, IMessageRepo
    {
        public SqlMessageRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
