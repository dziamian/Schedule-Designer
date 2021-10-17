using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ScheduleDesigner.Repositories.Interfaces
{
    public interface IProgrammeRepo : IRepoBase<Programme>
    {
        
    }
}
