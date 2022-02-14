using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.Base
{
    /// <summary>
    /// Implementacja wzorca generycznego repozytorium.
    /// </summary>
    /// <typeparam name="T">Klasa, której dotyczy wzorzec repozytorium</typeparam>
    public abstract class RepoBase<T> : IRepoBase<T> where T : class
    {
        /// <summary>
        /// Kontekst połączenia z bazą danych.
        /// </summary>
        protected ScheduleDesignerDbContext _context { get; set; }

        /// <summary>
        /// Konstruktor generycznego repozytorium.
        /// </summary>
        /// <param name="context">Instancja kontekstu połączenia z bazą danych</param>
        public RepoBase(ScheduleDesignerDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Zwraca kolekcję instancji klasy, której dotyczy repozytorium.
        /// </summary>
        /// <returns>Kolekcję instancji klasy, której dotyczy repozytorium</returns>
        public DbSet<T> GetAll()
        {
            if (_context == null)
            {
                return null;
            }

            return _context.Set<T>();
        }

        /// <summary>
        /// Zwraca kolekcję instancji spełniających podane wyrażenie.
        /// </summary>
        /// <param name="predicate">Wyrażenie będące kryterium wyszukiwania</param>
        /// <returns>Kolekcję instancji spełniających kryterium</returns>
        public IQueryable<T> Get(Expression<Func<T, bool>> predicate)
        {
            if (_context == null)
            {
                return null;
            }

            return _context.Set<T>()
                .Where(predicate);
        }

        /// <summary>
        /// Zwraca pierwszą instancję spełniającą podane wyrażenie.
        /// </summary>
        /// <param name="predicate">Wyrażenie będące kryterium wyszukiwania</param>
        /// <returns>Asynchroniczną operację przechowującą instancję spełniającą kryterium</returns>
        public async Task<T> GetFirst(Expression<Func<T, bool>> predicate)
        {
            if (_context == null)
            {
                return null;
            }

            return await _context.Set<T>()
                .FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// Funkcja zapisująca nową instancję klasy, której dotyczy repozytorium do jej kolekcji.
        /// </summary>
        /// <param name="entity">Instancja klasy, która ma zostać dodana do kolekcji</param>
        /// <returns>Nowo utworzoną instancję klasy</returns>
        public async Task<T> Add(T entity)
        {
            if (_context == null)
            {
                return null;
            }

            var result = await _context.Set<T>().AddAsync(entity);

            return result.Entity;
        }

        /// <summary>
        /// Funkcja zapisująca zmiany w instancji klasy, której dotyczy repozytorium.
        /// </summary>
        /// <param name="entity">Instancja klasy, która ma zostać nadpisana</param>
        /// <returns>Nadpisaną instancję klasy</returns>
        public T Update(T entity)
        {
            if (_context == null)
            {
                return null;
            }

            _context.Entry(entity).State = EntityState.Modified;

            return entity;
        }

        /// <summary>
        /// Funkcja usuwająca pierwszą instancję spełniającą podane wyrażenie.
        /// </summary>
        /// <param name="predicate">Wyrażenie będące kryterium wyszukiwania</param>
        /// <returns>Wartość 1 jeśli instancja została usunięta, w przeciwnym wypadku wartość -1</returns>
        public async Task<int> Delete(Expression<Func<T, bool>> predicate)
        {
            if (_context == null)
            {
                return -1;
            }

            var result = await _context.Set<T>()
                .FirstOrDefaultAsync(predicate);

            if (result == null)
            {
                return -1;
            }

            _context.Set<T>().Remove(result);
            return 1;
        }
    }
}
