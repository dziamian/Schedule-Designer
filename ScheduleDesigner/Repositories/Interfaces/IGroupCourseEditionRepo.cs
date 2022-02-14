using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;

namespace ScheduleDesigner.Repositories.Interfaces
{
    /// <summary>
    /// Interfejs będący rozszerzeniem generycznego repozytorium dla modelu <see cref="GroupCourseEdition"/>.
    /// </summary>
    public interface IGroupCourseEditionRepo : IRepoBase<GroupCourseEdition>
    {
    }
}
