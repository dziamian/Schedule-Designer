﻿using Microsoft.EntityFrameworkCore;
using ScheduleDesigner.Models;
using ScheduleDesigner.Repositories.Interfaces;
using ScheduleDesigner.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ScheduleDesigner.Repositories.Base;

namespace ScheduleDesigner.Repositories
{
    public class SettingsRepo : RepoBase<Settings>, ISettingsRepo
    {
        public SettingsRepo(ScheduleDesignerDbContext context)
            : base(context)
        { }
    }
}
