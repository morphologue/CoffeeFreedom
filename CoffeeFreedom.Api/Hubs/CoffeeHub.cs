using CoffeeFreedom.Common.Models;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace CoffeeFreedom.Api.Hubs
{
    public class CoffeeHub : Hub
    {
        public Task RespondAsync(WorkerResponse response)
        {
            return Task.CompletedTask;
        }
    }
}
