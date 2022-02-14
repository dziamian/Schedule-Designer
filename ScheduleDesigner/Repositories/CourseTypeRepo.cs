using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories
{
    /// <summary>
    /// Implementacja rozszerzonego repozytorium dla modelu <see cref="CourseType"/>.
    /// </summary>
    public class CourseTypeRepo : RepoBase<CourseType>, ICourseTypeRepo
    {
        /// <summary>
        /// Konstruktor rozszerzonego repozytorium.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
        public CourseTypeRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
