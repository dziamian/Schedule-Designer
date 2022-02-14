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
    /// Implementacja rozszerzonego repozytorium dla modelu <see cref="StudentGroup"/>.
    /// </summary>
    public class StudentGroupRepo : RepoBase<StudentGroup>, IStudentGroupRepo
    {
        /// <summary>
        /// Konstruktor rozszerzonego repozytorium.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
        public StudentGroupRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }

        /// <summary>
        /// Funkcja usuwająca wszystkie instancje spełniające podane wyrażenie.
        /// </summary>
        /// <param name="predicate">Wyrażenie będące kryterium wyszukiwania</param>
        /// <returns>Wartość 1 jeśli instancje zostały usunięte, w przeciwnym wypadku wartość -1</returns>
        public int DeleteMany(Func<StudentGroup, bool> predicate)
        {
            if (_context == null)
            {
                return -1;
            }

            var results = _context.Set<StudentGroup>()
                .Where(predicate);

            if (!results.Any())
            {
                return -1;
            }

            _context.Set<StudentGroup>().RemoveRange(results);
            return 1;
        }
    }
}
