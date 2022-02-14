using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;

namespace ScheduleDesigner.Repositories
{
    /// <summary>
    /// Implementacja rozszerzonego repozytorium dla modelu <see cref="CoordinatorCourseEdition"/>.
    /// </summary>
    public class CoordinatorCourseEditionRepo : RepoBase<CoordinatorCourseEdition>, ICoordinatorCourseEditionRepo
    {
        /// <summary>
        /// Konstruktor rozszerzonego repozytorium.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
        public CoordinatorCourseEditionRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
