using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class MessageRepo : RepoBase<Message>, IMessageRepo
    {
        public MessageRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
