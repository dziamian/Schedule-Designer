using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OData.Edm;

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
    }
}
