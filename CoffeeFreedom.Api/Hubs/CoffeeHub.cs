using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using CoffeeFreedom.Api.Services;
using CoffeeFreedom.Common.Models;
using Microsoft.AspNetCore.Authorization;

namespace CoffeeFreedom.Api.Hubs
{
    [Authorize]
    public class CoffeeHub : Hub<ICoffeeClient>
    {
        private readonly IWorkerResponseHandler _handler;

        public CoffeeHub(IWorkerResponseHandler handler)
        {
            _handler = handler;
        }

        public Task RespondAsync(WorkerResponse response)
        {
            _handler.HandleResponse(response);
            return Task.CompletedTask;
        }
    }

    public interface ICoffeeClient
    {
        Task RequestAsync(WorkerRequest request);
    }
}
