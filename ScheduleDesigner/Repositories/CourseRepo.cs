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
    /// Implementacja rozszerzonego repozytorium dla modelu <see cref="Course"/>.
    /// </summary>
    public class CourseRepo : RepoBase<Course>, ICourseRepo
    {
        /// <summary>
        /// Konstruktor rozszerzonego repozytorium.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
        public CourseRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
