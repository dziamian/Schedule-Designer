using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Authentication;
using ScheduleDesigner.Repositories.Base;

namespace ScheduleDesigner.Repositories.Interfaces
{
    /// <summary>
    /// Interfejs będący rozszerzeniem generycznego repozytorium dla modelu <see cref="Authorization"/>.
    /// </summary>
    public interface IAuthorizationRepo : IRepoBase<Authorization>
    {
    }
}
