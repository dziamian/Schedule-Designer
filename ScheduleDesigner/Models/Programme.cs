﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Models
{
    public class Programme
    {
        [Key]
        public int ProgrammeId { get; set; }

        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }


        public virtual ICollection<ProgrammeStage> ProgrammeStages { get; set; }
    }
}
