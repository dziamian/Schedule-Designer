using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories
{
    public class SqlSettingsRepo : ISettingsRepo
    {
        private readonly ScheduleDesignerDbContext _context;

        public SqlSettingsRepo(ScheduleDesignerDbContext context)
        {
            _context = context;
        }

        public async Task<Settings> GetSettings()
        {
            if (_context == null)
            {
                return null;
            }

            return await _context.Settings.SingleOrDefaultAsync();
        }

        public async Task<Settings> AddSettings(Settings settings)
        {
            if (_context == null)
            {
                return null;
            }
            
            var result = await _context.Settings.AddAsync(settings);
            await _context.SaveChangesAsync();

            return result.Entity;
        }

        public async Task<Settings> UpdateSettings(Settings settings)
        {
            if (_context == null)
            {
                return null;
            }

            _context.Entry(settings).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return settings;
        }

        public async Task<int> DeleteSettings()
        {
            if (_context == null)
            {
                return -1;
            }

            var _settings = await _context.Settings.SingleOrDefaultAsync();

            if (_settings == null)
            {
                return -1;
            }

            _context.Settings.Remove(_settings);
            
            return await _context.SaveChangesAsync();
        }

        public async Task<int> SaveChanges()
        {
            if (_context == null)
            {
                return -1;
            }

            return await _context.SaveChangesAsync();
        }
    }
}
