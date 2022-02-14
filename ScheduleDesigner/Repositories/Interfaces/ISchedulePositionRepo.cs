using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;

namespace ScheduleDesigner.Repositories.Interfaces
{
    /// <summary>
    /// Interfejs będący rozszerzeniem generycznego repozytorium dla modelu <see cref="SchedulePosition"/>.
    /// </summary>
    public interface ISchedulePositionRepo : IRepoBase<SchedulePosition>
    {
    }
}
