using System;
using System.Threading.Tasks;
using CoffeeFreedom.Common.Models;

namespace CoffeeFreedom.Worker
{
    internal class OrderRequestHandler : RequestHandlerBase
    {
        public async override Task<WorkerResponse> HandleAsync(WorkerRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
