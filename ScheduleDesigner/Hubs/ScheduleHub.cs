using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ScheduleDesigner.Hubs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ScheduleDesigner.Hubs
{
    [Authorize]
    public class ScheduleHub : Hub<IScheduleClient>
    {
        public override Task OnConnectedAsync()
        {
            //Console.WriteLine(Context.User.Claims.FirstOrDefault(claim => claim.Type == "user_id"));
            Context.User.Claims.ToList().ForEach(Console.WriteLine);
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            Console.WriteLine("Disconnected");
            //remove all locks from db for this user
            return base.OnDisconnectedAsync(exception);
        }
    }
}
