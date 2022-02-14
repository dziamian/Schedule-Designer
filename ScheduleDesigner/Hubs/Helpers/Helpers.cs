using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Hubs.Helpers
{
    /// <summary>
    /// Klasa wykorzystywana do przesyłania informacji o powodzeniu 
    /// lub niepowodzeniu operacji wykonywanych w centrum przez połączonego użytkownika.
    /// </summary>
    public class MessageObject
    {
        /// <summary>
        /// Kod statusu operacji (wzorowany na protokole HTTP)
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Dodatkowa wiadomość informująca o statusie operacji
        /// </summary>
        public string Message { get; set; }

        public override bool Equals(object obj)
        {
            return obj is MessageObject @object &&
                   StatusCode == @object.StatusCode &&
                   Message == @object.Message;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StatusCode, Message);
        }
    }
}
