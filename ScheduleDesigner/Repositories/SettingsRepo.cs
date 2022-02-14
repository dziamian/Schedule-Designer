using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Repositories.Base;

namespace ScheduleDesigner.Repositories
{
    /// <summary>
    /// Implementacja rozszerzonego repozytorium dla modelu <see cref="Settings"/>.
    /// </summary>
    public class SettingsRepo : RepoBase<Settings>, ISettingsRepo
    {
        /// <summary>
        /// Konstruktor rozszerzonego repozytorium.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
        public SettingsRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
