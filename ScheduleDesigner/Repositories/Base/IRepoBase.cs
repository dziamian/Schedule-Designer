using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.Base
{
    /// <summary>
    /// Interfejs wzorca generycznego repozytorium.
    /// </summary>
    /// <typeparam name="T">Klasa, której dotyczy wzorzec repozytorium</typeparam>
    public interface IRepoBase<T> where T : class
    {
        /// <summary>
        /// Zwraca kolekcję instancji klasy, której dotyczy repozytorium.
        /// </summary>
        /// <returns>Kolekcję instancji klasy, której dotyczy repozytorium</returns>
        DbSet<T> GetAll();

        /// <summary>
        /// Zwraca kolekcję instancji spełniających podane wyrażenie.
        /// </summary>
        /// <param name="predicate">Wyrażenie będące kryterium wyszukiwania</param>
        /// <returns>Kolekcję instancji spełniających kryterium</returns>
        IQueryable<T> Get(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Zwraca pierwszą instancję spełniającą podane wyrażenie.
        /// </summary>
        /// <param name="predicate">Wyrażenie będące kryterium wyszukiwania</param>
        /// <returns>Asynchroniczną operację przechowującą instancję spełniającą kryterium</returns>
        Task<T> GetFirst(Expression<Func<T, bool>> predicate);

        /// <summary>
        /// Funkcja zapisująca nową instancję klasy, której dotyczy repozytorium do jej kolekcji.
        /// </summary>
        /// <param name="entity">Instancja klasy, która ma zostać dodana do kolekcji</param>
        /// <returns>Nowo utworzoną instancję klasy</returns>
        Task<T> Add(T entity);

        /// <summary>
        /// Funkcja zapisująca zmiany w instancji klasy, której dotyczy repozytorium.
        /// </summary>
        /// <param name="entity">Instancja klasy, która ma zostać nadpisana</param>
        /// <returns>Nadpisaną instancję klasy</returns>
        T Update(T entity);

        /// <summary>
        /// Funkcja usuwająca pierwszą instancję spełniającą podane wyrażenie.
        /// </summary>
        /// <param name="predicate">Wyrażenie będące kryterium wyszukiwania</param>
        /// <returns>Wartość 1 jeśli instancja została usunięta, w przeciwnym wypadku wartość -1</returns>
        Task<int> Delete(Expression<Func<T, bool>> predicate);
    }
}
