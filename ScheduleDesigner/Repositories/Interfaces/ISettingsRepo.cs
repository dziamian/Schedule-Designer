using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.Interfaces
{
    public interface ISettingsRepo
    {
        Task<int> AddSettingsAsync(Settings settings);
        Task<Settings> GetSettingsAsync();
        Task<int> DeleteSettingsAsync();
        Task UpdateSettingsAsync(Settings settings);
        Task<int> SaveChangesAsync();
    }
}
