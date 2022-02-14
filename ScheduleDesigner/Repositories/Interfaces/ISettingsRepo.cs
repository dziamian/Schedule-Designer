using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.Interfaces
{
    /// <summary>
    /// Interfejs będący rozszerzeniem generycznego repozytorium dla modelu <see cref="Settings"/>.
    /// </summary>
    public interface ISettingsRepo : IRepoBase<Settings>
    {

    }
}
