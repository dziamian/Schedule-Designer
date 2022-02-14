using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    /// <summary>
    /// Implementacja rozszerzonego repozytorium dla modelu <see cref="Group"/>.
    /// </summary>
    public class GroupRepo : RepoBase<Group>, IGroupRepo
    {
        /// <summary>
        /// Konstruktor rozszerzonego repozytorium.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
        public GroupRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
