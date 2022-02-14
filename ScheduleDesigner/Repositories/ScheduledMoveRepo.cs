using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using ScheduleDesigner.Repositories.Interfaces;
using System;
using System.Linq;

namespace ScheduleDesigner.Repositories
{
    /// <summary>
    /// Implementacja rozszerzonego repozytorium dla modelu <see cref="ScheduledMove"/>.
    /// </summary>
    public class ScheduledMoveRepo : RepoBase<ScheduledMove>, IScheduledMoveRepo
    {
        /// <summary>
        /// Konstruktor rozszerzonego repozytorium.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
        public ScheduledMoveRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }

        /// <summary>
        /// Funkcja usuwająca wszystkie instancje spełniające podane wyrażenie.
        /// </summary>
        /// <param name="predicate">Wyrażenie będące kryterium wyszukiwania</param>
        /// <returns>Wartość 1 jeśli instancje zostały usunięte, w przeciwnym wypadku wartość -1</returns>
        public int DeleteMany(Func<ScheduledMove, bool> predicate)
        {
            if (_context == null)
            {
                return -1;
            }

            var results = _context.Set<ScheduledMove>()
                .Where(predicate).ToList();

            if (!results.Any())
            {
                return -1;
            }

            _context.Set<ScheduledMove>().RemoveRange(results);
            return 1;
        }
    }
}
