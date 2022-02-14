using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Authentication;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    /// <summary>
    /// Implementacja rozszerzonego repozytorium dla modelu <see cref="Authorization"/>.
    /// </summary>
    public class AuthorizationRepo : RepoBase<Authorization>, IAuthorizationRepo
    {
        /// <summary>
        /// Konstruktor rozszerzonego repozytorium.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
        public AuthorizationRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
