using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    public class StudentGroupRepo : RepoBase<StudentGroup>, IStudentGroupRepo
    {
        public StudentGroupRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }

        public int DeleteMany(Func<StudentGroup, bool> predicate)
        {
            if (_context == null)
            {
                return -1;
            }

            var results = _context.Set<StudentGroup>()
                .Where(predicate);

            if (!results.Any())
            {
                return -1;
            }

            _context.Set<StudentGroup>().RemoveRange(results);
            return 1;
        }
    }
}
