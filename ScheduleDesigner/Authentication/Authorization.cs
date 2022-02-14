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
    /// <summary>
    /// Model danych przechowujący informacje o tokenach dostępu i ich sekretach do zewnętrznego systemu USOS.
    /// </summary>
    public class Authorization
    {
        /// <summary>
        /// Identyfikator użytkownika.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Zahaszowany token dostępu do systemu USOS.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string AccessToken { get; set; }

        /// <summary>
        /// Zahaszowany sekret tokenu dostępu do systemu USOS.
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string AccessTokenSecret { get; set; }

        /// <summary>
        /// Data zapisania tokenu dostępu.
        /// </summary>
        public DateTime InsertedDateTime { get; set; }

        /// <summary>
        /// Reprezentacja relacji z użytkownikiem.
        /// </summary>
        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
