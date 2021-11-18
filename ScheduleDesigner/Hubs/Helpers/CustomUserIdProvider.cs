using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace ScheduleDesigner.Hubs.Helpers
{
    public class CustomUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User.Claims.FirstOrDefault(claim => claim.Type == "user_id")?.Value!;
        }
    }
}
