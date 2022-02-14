using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ScheduleDesigner.Hubs.Helpers
{
    /// <summary>
    /// Implementacja interfejsu konfigurującego identyfikator użytkownika dla połączenia z centrum SignalR.
    /// </summary>
    public class CustomUserIdProvider : IUserIdProvider
    {
        /// <summary>
        /// Zwraca identyfikator użytkownika dla kontekstu połączenia.
        /// </summary>
        /// <param name="connection">Kontekst indywidualnego połączenia z centrum</param>
        /// <returns>Identyfikator użytkownika dla połączenia</returns>
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!;
        }
    }
}
