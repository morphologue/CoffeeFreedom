using System;
using System.Threading.Tasks;
using CoffeeFreedom.Common.Models;

namespace CoffeeFreedom.Worker
{
    internal class PeekRequestHandler : RequestHandlerBase
    {
        public override async Task<WorkerResponse> HandleAsync(WorkerRequest request)
        {
            return await LogInAsync(request);
        }
    }
}
