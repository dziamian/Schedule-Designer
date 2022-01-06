using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using ScheduleDesigner.Models;

namespace ScheduleDesigner.Authentication
{
    public class Authorization
    {
        public int UserId { get; set; }

        [Required]
        [MaxLength(255)]
        public string AccessToken { get; set; }

        [Required]
        [MaxLength(255)]
        public string AccessTokenSecret { get; set; }

        public DateTime InsertedDateTime { get; set; }

        
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
