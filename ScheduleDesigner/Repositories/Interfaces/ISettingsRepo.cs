using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.Interfaces
{
    public interface ISettingsRepo
    {
        Task<int> AddSettings(Settings settings);
        Task<Settings> GetSettings();
        Task<int> DeleteSettings();
        Task UpdateSettings(Settings settings);
    }
}
