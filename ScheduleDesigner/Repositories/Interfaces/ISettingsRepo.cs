using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.Interfaces
{
    public interface ISettingsRepo
    {
        Task<Settings> GetSettings();
        Task<Settings> AddSettings(Settings settings);
        Task<Settings> UpdateSettings(Settings settings);
        Task<int> DeleteSettings();
        Task<int> SaveChanges();
    }
}
