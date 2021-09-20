using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Services
{
    public class ScheduleDesignerDbContext : DbContext
    {
        public DbSet<Settings> Settings { get; set; }

        public ScheduleDesignerDbContext(DbContextOptions<ScheduleDesignerDbContext> options) : base(options)
        {

        }
    }
}
