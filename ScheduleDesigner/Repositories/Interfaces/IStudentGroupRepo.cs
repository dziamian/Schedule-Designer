using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;

namespace ScheduleDesigner.Repositories.Interfaces
{
    /// <summary>
    /// Interfejs będący rozszerzeniem generycznego repozytorium dla modelu <see cref="StudentGroup"/>.
    /// </summary>
    public interface IStudentGroupRepo : IRepoBase<StudentGroup>
    {
        /// <summary>
        /// Funkcja usuwająca wszystkie instancje spełniające podane wyrażenie.
        /// </summary>
        /// <param name="predicate">Wyrażenie będące kryterium wyszukiwania</param>
        /// <returns>Wartość 1 jeśli instancje zostały usunięte, w przeciwnym wypadku wartość -1</returns>
        public int DeleteMany(Func<StudentGroup, bool> predicate);
    }
}
